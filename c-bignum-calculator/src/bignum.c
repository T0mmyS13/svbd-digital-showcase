#include "bignum.h"
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include <stdio.h>

#define BIGNUM_BASE 256
#define BITS_PER_BYTE 8
#define HEX_MASK 0x0F
#define SIGN_BIT_MASK 0x80
#define HEX_SIGN_BIT 0x08
#define BITS_PER_HEX_DIGIT 4

/* ================================================================== */
/* === INTERNI POMOCNE FUNKCE === */
/* ================================================================== */

/**
 * \brief Prevede hexadecimalni znak na hodnotu 0-15.
 * \param c Znak k prevodu.
 * \return Hodnota 0-15, nebo -1 pri chybe.
 */
static int _hex_char_to_val(char c)
{
    if (c >= '0' && c <= '9') {
        return c - '0';
    }
    if (c >= 'a' && c <= 'f') {
        return c - 'a' + 10;
    }
    if (c >= 'A' && c <= 'F') {
        return c - 'A' + 10;
    }
    return -1;
}

/**
 * \brief Prevede hodnotu 0-HEX_MASK na hexadecimalni znak.
 * \param val Hodnota k prevodu.
 * \return Znak '0'-'9' nebo 'a'-'f', nebo '?' pri chybe.
 */
static char _val_to_hex_char(int val)
{
    if (val >= 0 && val <= 9) {
        return (char)(val + '0');
    }
    if (val >= 10 && val <= HEX_MASK) {
        return (char)(val - 10 + 'a');
    }
    return '?';
}

/**
 * \brief Zjisti, jestli je cislo nula.
 * \param n Ukazatel na strukturu BigNum.
 * \return 1 pokud je cislo nula, jinak 0.
 */
static int _bignum_is_zero(const struct BigNum *n)
{
    return (n->size == 1 && n->bytes[0] == 0);
}

/**
 * \brief Zjisti, jestli je cislo liche.
 * \param n Ukazatel na strukturu BigNum.
 * \return 1 pokud je cislo liche, jinak 0.
 */
static int _bignum_is_odd(const struct BigNum *n)
{
    if (n->size > 0 && (n->bytes[0] & 1)) {
        return 1;
    }
    return 0;
}

/**
 * \brief Zdvojnasobi kapacitu pameti pro bajty v BigNum.
 * \param n Ukazatel na strukturu BigNum.
 * \return EXIT_SUCCESS pri uspechu, EXIT_FAILURE pri chybe alokace.
 */
static int _bignum_grow(struct BigNum *n)
{
    size_t new_capacity = n->capacity == 0 ? 1 : n->capacity * 2;
    void *new_bytes = realloc(n->bytes, new_capacity * sizeof(unsigned char));
    if (!new_bytes) {
        return EXIT_FAILURE;
    }

    memset((char *)new_bytes + n->capacity, 0, (new_capacity - n->capacity) * sizeof(unsigned char));

    n->bytes = (unsigned char *)new_bytes;
    n->capacity = new_capacity;
    return EXIT_SUCCESS;
}

/**
 * \brief Nastavi BigNum na hodnotu 0.
 * \param n Ukazatel na strukturu BigNum.
 */
static void _bignum_set_zero(struct BigNum *n)
{
    if (n->capacity == 0 || n->bytes == NULL) {
        if (bignum_init(n, 1) != EXIT_SUCCESS) {
            return;
        }
    }
    memset(n->bytes, 0, n->capacity);
    n->size = 1;
    n->is_negative = 0;
}

/**
 * \brief Vydeli BigNum malym cislem a vrati zbytek.
 * \param n Ukazatel na BigNum (modifikuje se).
 * \param divisor Delitel (male cislo, typicky 10).
 * \return Zbytek po deleni.
 */
static unsigned int _bignum_div_by_small(struct BigNum *n, unsigned int divisor)
{
    long i;
    unsigned int remainder = 0;
    unsigned int value;
    
    for (i = (long)n->size - 1; i >= 0; i--)
    {
        value = (unsigned int)n->bytes[i] + (remainder * BIGNUM_BASE);
        n->bytes[i] = (unsigned char)(value / divisor);
        remainder = value % divisor;
    }

    while (n->size > 1 && n->bytes[n->size - 1] == 0) {
        n->size--;
    }
    
    return remainder;
}

/**
 * \brief Jadro odcitani: result = a - b.
 * Predpoklada, ze A i B jsou kladne a A >= B.
 * \param result Ukazatel na vysledne BigNum.
 * \param a Ukazatel na mensenec.
 * \param b Ukazatel na mensitel.
 * \return CALC_OK pri uspechu, jinak chybovy kod.
 */
static enum CalcError _bignum_sub_core(struct BigNum *result, const struct BigNum *a, const struct BigNum *b)
{
    size_t i;
    int borrow = 0;
    int temp_diff;
    struct BigNum temp_result;

    if (bignum_init(&temp_result, a->size) != EXIT_SUCCESS) {
        return CALC_MALLOC_FAIL;
    }

    for (i = 0; i < a->size; i++)
    {
        int byte_a = a->bytes[i];
        int byte_b = (i < b->size) ? b->bytes[i] : 0;

        temp_diff = byte_a - byte_b - borrow;

        if (temp_diff < 0) {
            temp_diff += BIGNUM_BASE; 
            borrow = 1;       
        } else {
            borrow = 0;
        }
        temp_result.bytes[i] = (unsigned char)temp_diff;
    }

    temp_result.size = a->size;
    while (temp_result.size > 1 && temp_result.bytes[temp_result.size - 1] == 0) {
        temp_result.size--;
    }
    temp_result.is_negative = 0;

    bignum_deinit(result);
    *result = temp_result;

    return CALC_OK;
}

/**
 * \brief Porovna absolutni hodnoty dvou BigNum.
 * \param a Ukazatel na prvni BigNum.
 * \param b Ukazatel na druhe BigNum.
 * \return 1 (|a| > |b|), -1 (|a| < |b|), 0 (|a| == |b|)
 */
static int _bignum_compare_abs(const struct BigNum *a, const struct BigNum *b)
{
    long i;

    if (a->size > b->size) {
        return 1;
    }
    if (a->size < b->size) {
        return -1;
    }

    for (i = (long)a->size - 1; i >= 0; i--) {
        if (a->bytes[i] > b->bytes[i]) {
            return 1;
        }
        if (a->bytes[i] < b->bytes[i]) {
            return -1;
        }
    }
    return 0;
}

/**
 * \brief Nastavi BigNum na malou kladnou celociselnou hodnotu.
 * \param n Ukazatel na strukturu BigNum.
 * \param value Hodnota, na kterou se ma BigNum nastavit.
 * \return EXIT_SUCCESS pri uspechu, EXIT_FAILURE pri chybe.
 */
static int _bignum_from_int(struct BigNum *n, unsigned int value)
{
    if (bignum_init(n, sizeof(unsigned int)) != EXIT_SUCCESS) {
        return EXIT_FAILURE;
    }

    if (value == 0) {
        return EXIT_SUCCESS;
    }

    n->size = 0; 
    while (value > 0)
    {
        if (n->size >= n->capacity) {
            if (_bignum_grow(n) != EXIT_SUCCESS) {
                return EXIT_FAILURE;
            }
        }
        n->bytes[n->size] = (unsigned char)(value % BIGNUM_BASE);
        n->size++;
        value = value / BIGNUM_BASE;
    }
    return EXIT_SUCCESS;
}

/**
 * \brief Prevede cislo ve dvojkovem doplnku na absolutni hodnotu.
 * \param n Ukazatel na strukturu BigNum.
 * \return EXIT_SUCCESS pri uspechu, EXIT_FAILURE pri chybe.
 */
static int _bignum_twos_complement_to_abs(struct BigNum *n)
{
    struct BigNum one;
    int one_inited = 0;
    int ret = EXIT_SUCCESS;
    size_t i;

    /* Invertuje bity */
    for (i = 0; i < n->size; i++) {
        n->bytes[i] = ~n->bytes[i];
    }
    n->is_negative = 0;

    if (_bignum_from_int(&one, 1) != EXIT_SUCCESS) {
        ret = EXIT_FAILURE; goto cleanup_twos;
    }
    one_inited = 1;

    if (bignum_add(n, n, &one) != EXIT_SUCCESS) {
        ret = EXIT_FAILURE; goto cleanup_twos;
    }

cleanup_twos:
    if (one_inited) {
        bignum_deinit(&one);
    }
    n->is_negative = 1;
    return ret;
}

/**
 * \brief Vynasobi BigNum dvema (bitovy posun doleva).
 * \param n Ukazatel na strukturu BigNum.
 * \return CALC_OK pri uspechu, CALC_MALLOC_FAIL pri chybe.
 */
static int _bignum_shift_left(struct BigNum *n)
{
    size_t i;
    unsigned int carry = 0;
    unsigned int temp;

    if (n->size > 0 && (n->bytes[n->size - 1] & SIGN_BIT_MASK)) {
        if (n->size >= n->capacity) {
            if (_bignum_grow(n) != EXIT_SUCCESS) {
                return CALC_MALLOC_FAIL;
            }
        }
    }

    for (i = 0; i < n->size; i++)
    {
        temp = ((unsigned int)n->bytes[i] << 1) + carry;
        n->bytes[i] = (unsigned char)(temp % BIGNUM_BASE);
        carry = temp / BIGNUM_BASE;
    }

    if (carry > 0) {
        n->bytes[n->size] = (unsigned char)carry;
        n->size++;
    }
    return CALC_OK;
}

/**
 * \brief Spocita podil a zbytek pomoci Long Division.
 * \param quotient Ukazatel na vysledny podil.
 * \param remainder Ukazatel na vysledny zbytek.
 * \param a Ukazatel na delenec.
 * \param b Ukazatel na delitel.
 * \return CALC_OK pri uspechu, jinak chybovy kod.
 */
static enum CalcError _bignum_div_core (struct BigNum *quotient, 
                                                 struct BigNum *remainder,
                                                 const struct BigNum *a, const struct BigNum *b)
{
    struct BigNum b_abs;
    struct BigNum temp_quotient_bit;
    int ret = CALC_OK;
    long i, k;
    int q_inited = 0, r_inited = 0, b_inited = 0, q_bit_inited = 0;

    if (_bignum_is_zero(b)) {
        return CALC_DIV_ZERO;
    }

    if (bignum_init(quotient, a->size) != EXIT_SUCCESS) {
        ret = CALC_MALLOC_FAIL;
        goto cleanup_divmod;
    }
    q_inited = 1;
    if (bignum_init(remainder, b->size) != EXIT_SUCCESS) {
        ret = CALC_MALLOC_FAIL;
        goto cleanup_divmod;
    }
    r_inited = 1;
    if (bignum_copy(&b_abs, b) != EXIT_SUCCESS) {
        ret = CALC_MALLOC_FAIL;
        goto cleanup_divmod;
    }
    b_abs.is_negative = 0;
    b_inited = 1;
    if (_bignum_from_int(&temp_quotient_bit, 1) != EXIT_SUCCESS) {
        ret = CALC_MALLOC_FAIL;
        goto cleanup_divmod;
    }
    q_bit_inited = 1;

    /* Long Division po bitech */
    for (i = (long)a->size - 1; i >= 0; i--)
    {
        for (k = BITS_PER_BYTE - 1; k >= 0; k--)
            {
            _bignum_shift_left(remainder);
            if ((a->bytes[i] >> k) & 1) {
                bignum_add(remainder, remainder, &temp_quotient_bit);
            }
            
            if (_bignum_compare_abs(remainder, &b_abs) >= 0)
            {
                size_t bit_index = (size_t)i * BITS_PER_BYTE + (size_t)k;
                size_t byte_index = (size_t)(bit_index / BITS_PER_BYTE);
                int bit_pos = (int)(bit_index % BITS_PER_BYTE);

                _bignum_sub_core(remainder, remainder, &b_abs);
                
                if (quotient->size <= byte_index) { quotient->size = byte_index + 1; }
                quotient->bytes[byte_index] |= (unsigned char)(1u << bit_pos);
            }
        }
    }
    
    while (quotient->size > 1 && quotient->bytes[quotient->size - 1] == 0) {
        quotient->size--;
    }
    
    quotient->is_negative = (a->is_negative != b->is_negative) && !_bignum_is_zero(quotient);
    remainder->is_negative = a->is_negative && !_bignum_is_zero(remainder);

cleanup_divmod:
    if (b_inited) {
        bignum_deinit(&b_abs);
    }
    if (q_bit_inited) {
        bignum_deinit(&temp_quotient_bit);
    }
    if (ret != CALC_OK) {
        if (q_inited) {
            bignum_deinit(quotient);
        }
        if (r_inited) {
            bignum_deinit(remainder);
        }
    }
    return ret;
}

/**
 * \brief Snizi hodnotu 'n' o 1 (n = n - 1).
 * \param n Ukazatel na BigNum.
 */
static void _bignum_dec_one(struct BigNum *n)
{
    size_t i;
    int borrow = 1;
    int temp_diff;

    for (i = 0; i < n->size; i++)
    {
        temp_diff = n->bytes[i] - borrow;

        if (temp_diff < 0) {
            temp_diff += BIGNUM_BASE;
            borrow = 1; 
        } else {
            borrow = 0;
        }
        n->bytes[i] = (unsigned char)temp_diff;
        if (borrow == 0) {
            break;
        }
    }

    while (n->size > 1 && n->bytes[n->size - 1] == 0) {
        n->size--;
    }
}

/**
 * \brief Prida prefix (0b/0x) a znamenko k otocenemu retezci.
 * \param reversed_str Otoceny retezec s cislem.
 * \param base Ciselna soustava.
 * \param is_negative Priznak, zda je cislo zaporne.
 * \return Ukazatel na nove alokovany retezec, nebo NULL pri chybe.
 */
static char *_bignum_to_string_finalize(char *reversed_str, int base, int is_negative)
{
    char *result_str = NULL;
    size_t len = strlen(reversed_str);
    size_t prefix_len = 0;
    size_t sign_len = 0;
    char *p;

    if (base == BASE_BIN) {
        prefix_len = 2;
    } else if (base == BASE_HEX) {
        prefix_len = 2;
    }
    
    if (base == BASE_DEC && is_negative) {
        sign_len = 1;
    }

    /* len cislic + prefix (0b/0x) + znamenko (-) + null terminator */
    result_str = (char *)malloc(len + prefix_len + sign_len + 1);
    if (!result_str) {
        return NULL;
    }
    
    p = result_str;
    if (sign_len > 0) *p++ = '-';
    if (prefix_len > 0) {
        *p++ = '0';
        *p++ = (base == BASE_BIN ? 'b' : 'x');
    }

    while (len > 0) {
        *p++ = reversed_str[--len];
    }
    *p = '\0';
    return result_str;
}

/**
 * \brief Prevede BigNum na desitkovy retezec (vraci pozpatku).
 * \param n Ukazatel na BigNum k prevodu.
 * \return Ukazatel na alokovany retezec (pozpatku), nebo NULL.
 */
static char *_bignum_to_string_dec(const struct BigNum *n)
{
    struct BigNum temp_n;
    char *temp_str = NULL; 
    size_t str_len = 0;
    int ret = EXIT_SUCCESS;
    int temp_n_inited = 0;
    unsigned int digit;

    /* Max 3 cifry na bajt (255), +1 znamenko, +1 null terminator */
    temp_str = (char *)malloc(n->size * 3 + 2);
    if (!temp_str) {
        ret = EXIT_FAILURE;
        goto cleanup_tostr_dec;
    }

    if (bignum_copy(&temp_n, n) != EXIT_SUCCESS) {
        ret = EXIT_FAILURE;
        goto cleanup_tostr_dec;
    }
    temp_n_inited = 1;
    temp_n.is_negative = 0;

    digit = _bignum_div_by_small(&temp_n, BASE_DEC);
    temp_str[str_len++] = (char)(digit + '0');
    
    while (!_bignum_is_zero(&temp_n)) {
        digit = _bignum_div_by_small(&temp_n, BASE_DEC);
        temp_str[str_len++] = (char)(digit + '0');
    }

    temp_str[str_len] = '\0';

cleanup_tostr_dec:
    if (temp_n_inited) {
        bignum_deinit(&temp_n);
    }
    
    if (ret == EXIT_FAILURE) {
        free(temp_str);
        return NULL;
    }
    return temp_str;
}

/**
 * \brief Prevede BigNum na bin/hex retezec (vraci pozpatku).
 * Pouziva dvojkovy doplnek pro zaporna cisla.
 * \param n Ukazatel na BigNum k prevodu.
 * \param base Ciselna soustava (BASE_BIN nebo BASE_HEX).
 * \return Ukazatel na alokovany retezec (pozpatku), nebo NULL.
 */
static char *_bignum_to_string_twos_complement(const struct BigNum *n, int base)
{
    char *temp_str = NULL;
    size_t str_len = 0;
    size_t i, k;
    int sign_bit = (n->is_negative ? 1 : 0);
    unsigned char byte;
    
    struct BigNum n_copy = DEFAULT_BIGNUM;
    int n_copy_inited = 0;
    const struct BigNum *num_to_print;

    /* 8 bitu na bajt + rezerva pro sign extension + null terminator */
    temp_str = (char *)malloc(n->size * BITS_PER_BYTE + 2); 
    if (!temp_str) {
        return NULL;
    }

    /* Převod na dvojkový doplněk pro záporná čísla */
    if (n->is_negative) {
        if (bignum_copy(&n_copy, n) != EXIT_SUCCESS) { 
            goto cleanup_tostr_twos; 
        }
        n_copy_inited = 1;
        
        _bignum_dec_one(&n_copy);
        for (i = 0; i < n_copy.size; i++) {
            n_copy.bytes[i] = ~n_copy.bytes[i];
        }
        num_to_print = &n_copy;
    } else {
        num_to_print = n;
    }
    
    /* Převod na řetězec (pozpátku) */
    for (i = 0; i < num_to_print->size; i++)
    {
        byte = num_to_print->bytes[i];
        
        if (base == BASE_BIN) {
            for (k = 0; k < BITS_PER_BYTE; k++) {
                int bit = (byte >> k) & 1;
                temp_str[str_len++] = (char)(bit + '0');
            }
        } else { /* base == BASE_HEX */
            char c1 = _val_to_hex_char(byte & HEX_MASK);
            char c2 = _val_to_hex_char((byte >> BITS_PER_HEX_DIGIT) & HEX_MASK);

            if (c1 == '?' || c2 == '?') {
                free(temp_str);
                if (n_copy_inited) bignum_deinit(&n_copy);
                return NULL;
            }
            temp_str[str_len++] = c1;
            temp_str[str_len++] = c2;
        }
    }

    /* Oříznutí podle dvojkového doplňku */
    while (str_len > 1)
    {
        char last_char = temp_str[str_len - 1];
        char prev_char = temp_str[str_len - 2];
        int last_val, prev_val;

        if (base == BASE_BIN) {
            last_val = last_char - '0';
            prev_val = prev_char - '0';
            
            if (last_val == sign_bit && prev_val == sign_bit) {
                str_len--;
            } else {
                break;
            }
        } else { /* base == BASE_HEX */
            last_val = _hex_char_to_val(last_char);
            prev_val = _hex_char_to_val(prev_char);
            
            if (sign_bit == 0 && last_val == 0 && (prev_val & HEX_SIGN_BIT) == 0) {
                 str_len--;
            } else if (sign_bit == 1 && last_val == HEX_MASK && (prev_val & HEX_SIGN_BIT) != 0) {
                 str_len--;
            } else {
                break;
            }
        }
    }
    
    /* Pokud je číslo kladné (sign_bit == 0), musíme zajistit,
     * že první znak (který je teď na konci) NENÍ 1.
     * Pokud by byl, musíme přidat jednu 0 navíc.
     */
    if (sign_bit == 0) {
        char last_char = temp_str[str_len - 1];
        int first_val;
        if (base == BASE_BIN) {
            first_val = last_char - '0';
            if (first_val == 1) temp_str[str_len++] = '0';
        } else { /* base == BASE_HEX */
            first_val = _hex_char_to_val(last_char);
            if (first_val >= HEX_SIGN_BIT) temp_str[str_len++] = '0';
        }
    }
    else {
        char last_char = temp_str[str_len - 1];
        int first_val;
        if (base == BASE_BIN) {
            first_val = last_char - '0';
            if (first_val == 0) temp_str[str_len++] = '1';
        } else { /* base == BASE_HEX */
            first_val = _hex_char_to_val(last_char);
            if (first_val < HEX_SIGN_BIT) temp_str[str_len++] = 'f';
        }
    }
    
    temp_str[str_len] = '\0';

cleanup_tostr_twos:
    if (n_copy_inited) bignum_deinit(&n_copy);
    return temp_str;
}

/**
 * \brief Rychle nastavi jiz inicializovany BigNum na malou hodnotu.
 * \param n Ukazatel na strukturu BigNum.
 * \param value Hodnota, na kterou se ma BigNum nastavit.
 */
static void _bignum_set_int(struct BigNum *n, unsigned int value)
{
    n->is_negative = 0;
    n->size = 0;

    if (value == 0) {
        n->size = 1;
        n->bytes[0] = 0;
        return;
    }

    while (value > 0)
    {
        n->bytes[n->size] = (unsigned char)(value % BIGNUM_BASE);
        n->size++;
        value = value / BIGNUM_BASE;
    }
}

/**
 * \brief Zpracuje desitkovy retezec na BigNum.
 * \param n Ukazatel na strukturu BigNum.
 * \param start Ukazatel na zacatek retezce.
 * \param is_negative_sign Priznak zaporneho znamenka.
 * \return EXIT_SUCCESS pri uspechu, EXIT_FAILURE pri chybe.
 */
static int _bignum_from_string_dec(struct BigNum *n, const char *start, int is_negative_sign)
{
    size_t len = strlen(start);
    size_t i;
    int ret = EXIT_SUCCESS;
    
    struct BigNum ten;
    struct BigNum digit;
    int ten_inited = 0, digit_inited = 0, n_inited = 0;

    if (_bignum_from_int(&ten, BASE_DEC) != EXIT_SUCCESS) {
        ret = EXIT_FAILURE;
        goto cleanup;
    }
    ten_inited = 1;

    if (bignum_init(&digit, 1) != EXIT_SUCCESS) {
        ret = EXIT_FAILURE;
        goto cleanup;
    }
    digit_inited = 1;

    if (bignum_init(n, (len / 3) + 1) != EXIT_SUCCESS) {
        ret = EXIT_FAILURE;
        goto cleanup;
    }
    n_inited = 1;

    for (i = 0; i < len; i++)
    {
        unsigned int digit_val;

        if (start[i] >= '0' && start[i] <= '9') {
            digit_val = (unsigned int)(start[i] - '0');
        } else {
            ret = EXIT_FAILURE;
            goto cleanup;
        }

        if (bignum_mul(n, n, &ten) != EXIT_SUCCESS) {
            ret = EXIT_FAILURE;
            goto cleanup;
        }
        _bignum_set_int(&digit, digit_val);
        if (bignum_add(n, n, &digit) != EXIT_SUCCESS) {
            ret = EXIT_FAILURE;
            goto cleanup;
        }
    }

cleanup:
    if (ten_inited) {
        bignum_deinit(&ten);
    }
    if (digit_inited) {
        bignum_deinit(&digit);
    }

    if (ret != EXIT_SUCCESS && n_inited) {
        bignum_deinit(n);
    } else if (n_inited) {
        n->is_negative = is_negative_sign;
    }
    return ret;
}

/**
 * \brief Zpracuje binarni nebo hex retezec (dvojkovy doplnek).
 * \param n Ukazatel na strukturu BigNum.
 * \param start Ukazatel na zacatek retezce.
 * \param base Ciselna soustava (2 nebo 16).
 * \return EXIT_SUCCESS pri uspechu, EXIT_FAILURE pri chybe.
 */
static int _bignum_from_string_twos_complement(struct BigNum *n, const char *start, int base)
{
    int is_negative_twos_complement = 0;
    size_t len = strlen(start);
    long i;
    unsigned char current_byte = 0;
    int bit_count = 0;
    int val;

    /* 2 hex znaky = 1 bajt, +1 rezerva */
    if (bignum_init(n, (len / 2) + 1) != EXIT_SUCCESS) 
        return EXIT_FAILURE;

    /* Detekce znaménka */
    if (base == BASE_BIN) {
        if (*start == '1') is_negative_twos_complement = 1;
    } else { /* base == BASE_HEX */
        val = _hex_char_to_val(*start);
        if (val == -1) { 
            bignum_deinit(n); 
            return EXIT_FAILURE; 
        } 
        if (val >= HEX_SIGN_BIT) 
            is_negative_twos_complement = 1;
    }
    n->is_negative = is_negative_twos_complement;

    /* Parsování odzadu */
    n->size = 0;
    for (i = (long)len - 1; i >= 0; i--)
    {
        if (base == BASE_BIN) {
            if (start[i] == '1') {
                current_byte |= (1 << bit_count);
            } else if (start[i] != '0') {
                bignum_deinit(n); 
                return EXIT_FAILURE;
            }
            bit_count++;
        } else { /* base == BASE_HEX */
            val = _hex_char_to_val(start[i]);  
            if (val == -1) { 
                bignum_deinit(n); 
                return EXIT_FAILURE; 
            }           
            current_byte |= (val << bit_count);
            bit_count += BITS_PER_HEX_DIGIT;
        }

        if (bit_count >= BITS_PER_BYTE)
        {
            if (n->size >= n->capacity) {
                if (_bignum_grow(n) != EXIT_SUCCESS) { 
                    bignum_deinit(n); 
                    return EXIT_FAILURE; 
                }
            }
            n->bytes[n->size] = current_byte;
            n->size++;
            current_byte = 0;
            bit_count = 0;
        }
    } 

    /* Zpracování posledního bajtu a sign extension */
    if (bit_count > 0 || n->size == 0)
    {
        if (is_negative_twos_complement) {
            while (bit_count < BITS_PER_BYTE) {
                current_byte |= (1 << bit_count);
                bit_count++;
            }
        }
        if (n->size >= n->capacity) {
            if (_bignum_grow(n) != EXIT_SUCCESS) {
                bignum_deinit(n);
                return EXIT_FAILURE;
            }
        }
        n->bytes[n->size] = current_byte;
        n->size++;
    }

    if (is_negative_twos_complement)
    {
        if (_bignum_twos_complement_to_abs(n) != EXIT_SUCCESS)
        {
            bignum_deinit(n);
            return EXIT_FAILURE;
        }
    }
    
    while (n->size > 1 && n->bytes[n->size - 1] == 0) {
        n->size--;
    }

    return EXIT_SUCCESS;
}

/* ================================================================== */
/* === SPRAVA PAMETI === */
/* ================================================================== */

int bignum_init(struct BigNum *n, size_t capacity)
{
    if (!n || capacity == 0) {
        return EXIT_FAILURE;
    }
    
    n->bytes = (unsigned char *)malloc(capacity * sizeof(unsigned char));
    if (!n->bytes) {
        return EXIT_FAILURE;
    }

    memset(n->bytes, 0, capacity * sizeof(unsigned char));
    n->capacity = capacity;
    n->size = 1;
    n->is_negative = 0;
    return EXIT_SUCCESS;
}

void bignum_deinit(struct BigNum *n)
{
    if (n) {
        free(n->bytes);
        n->bytes = NULL;
        n->capacity = 0;
        n->size = 0;
        n->is_negative = 0;
    }
}

int bignum_copy(struct BigNum *dest, const struct BigNum *source)
{
    if (!dest || !source) {
        return EXIT_FAILURE;
    }

    if (bignum_init(dest, source->size) != EXIT_SUCCESS) {
        return EXIT_FAILURE; 
    }
    
    memcpy(dest->bytes, source->bytes, source->size);
    dest->size = source->size;
    dest->is_negative = source->is_negative;
    return EXIT_SUCCESS;
}


/* ================================================================== */
/* === KONVERZE === */
/* ================================================================== */

int bignum_from_string(struct BigNum *n, const char *str)
{
    int base = BASE_DEC;
    int is_negative_sign = 0;
    const char *start;
    size_t len;

    if (!n || !str) {
        return EXIT_FAILURE;
    }

    start = str;
    if (*start == '-') {
        is_negative_sign = 1;
        start++; 
    } else if (*start == '+') {
        start++;
    }

    if (strncmp(start, "0b", 2) == 0 || strncmp(start, "0B", 2) == 0) {
        base = BASE_BIN;
        start += 2;
    } else if (strncmp(start, "0x", 2) == 0 || strncmp(start, "0X", 2) == 0) {
        base = BASE_HEX;
        start += 2;
    } else {
        base = BASE_DEC;
    }

    if (is_negative_sign && base != BASE_DEC) {
        return EXIT_FAILURE;
    }

    len = strlen(start);
    if (len == 0) {
        return EXIT_FAILURE;
    }

    if (base == BASE_DEC) {
        return _bignum_from_string_dec(n, start, is_negative_sign);
    } else {
        return _bignum_from_string_twos_complement(n, start, base);
    }
}

char *bignum_to_string(const struct BigNum *n, int base)
{
    char *reversed_str = NULL;
    char *final_str = NULL;

    if (!n) {
        return NULL;
    }

    if (_bignum_is_zero(n)) {
        final_str = (char *)malloc(4); /* dostatecne pro "0", "0b0" nebo "0x0" */
        if (!final_str) {
            return NULL;
        }
        
        if (base == BASE_DEC) {
            strcpy(final_str, "0");
        } else if (base == BASE_BIN) {
            strcpy(final_str, "0b0");
        } else if (base == BASE_HEX) {
            strcpy(final_str, "0x0");
        } else {
            strcpy(final_str, "0");
        }
        
        return final_str;
    }

    if (base == BASE_DEC) {
        reversed_str = _bignum_to_string_dec(n);
    } else if (base == BASE_BIN || base == BASE_HEX) {
        reversed_str = _bignum_to_string_twos_complement(n, base);
    } else {
        return NULL;
    }

    if (!reversed_str) {
        return NULL;
    }

    final_str = _bignum_to_string_finalize(reversed_str, base, n->is_negative);
    free(reversed_str);
    
    return final_str;
}

/* ================================================================== */
/* === ARITMETICKÉ OPERACE === */
/* ================================================================== */

enum CalcError bignum_add(struct BigNum *result, const struct BigNum *a, const struct BigNum *b)
{
    int ret;
    size_t max_size, i;
    unsigned int carry = 0;
    unsigned int temp_sum;
    struct BigNum temp_result;
    
    if (!result || !a || !b) {
        return CALC_INVALID_ARG;
    }

    /* a(+) + b(-) -> a - |b| */
    if (!a->is_negative && b->is_negative) {
        struct BigNum b_abs;
        if (bignum_copy(&b_abs, b) != EXIT_SUCCESS) {
            return CALC_MALLOC_FAIL;
        }
        b_abs.is_negative = 0;
        ret = bignum_sub(result, a, &b_abs);
        bignum_deinit(&b_abs);
        return ret;
    }

    /* a(-) + b(+) -> b - |a| */
    if (a->is_negative && !b->is_negative) {
        struct BigNum a_abs;
        if (bignum_copy(&a_abs, a) != EXIT_SUCCESS) {
            return CALC_MALLOC_FAIL;
        }
        a_abs.is_negative = 0;
        ret = bignum_sub(result, b, &a_abs);
        bignum_deinit(&a_abs);
        return ret;
    }

    max_size = (a->size > b->size) ? a->size : b->size;
    if (bignum_init(&temp_result, max_size + 1) != EXIT_SUCCESS) {
        return CALC_MALLOC_FAIL;
    }

    for (i = 0; i < max_size; i++)
    {
        unsigned int byte_a = (i < a->size) ? a->bytes[i] : 0;
        unsigned int byte_b = (i < b->size) ? b->bytes[i] : 0;

        temp_sum = byte_a + byte_b + carry;
        temp_result.bytes[i] = (unsigned char)(temp_sum % BIGNUM_BASE);
        carry = temp_sum / BIGNUM_BASE;
    }

    if (carry > 0) {
        temp_result.bytes[i] = (unsigned char)carry;
        temp_result.size = i + 1;
    } else {
        temp_result.size = i;
    }
    
    if (temp_result.size == 0) temp_result.size = 1;
    temp_result.is_negative = a->is_negative;

    bignum_deinit(result);
    *result = temp_result;

    return CALC_OK;
}

enum CalcError bignum_sub(struct BigNum *result, const struct BigNum *a, const struct BigNum *b)
{
    int comparison;

    if (!result || !a || !b) {
        return CALC_INVALID_ARG;
    }

    /* a(+) - b(-) -> a + |b| */
    if (!a->is_negative && b->is_negative) {
        struct BigNum b_abs;
        int ret;
        if (bignum_copy(&b_abs, b) != EXIT_SUCCESS) {
            return CALC_MALLOC_FAIL;
        }
        b_abs.is_negative = 0;
        ret = bignum_add(result, a, &b_abs);
        bignum_deinit(&b_abs);
        return ret;
    }

    /* a(-) - b(+) -> -( |a| + b ) */
    if (a->is_negative && !b->is_negative) {
        struct BigNum a_abs;
        int ret;
        if (bignum_copy(&a_abs, a) != EXIT_SUCCESS) {
            return CALC_MALLOC_FAIL;
        }
        a_abs.is_negative = 0;
        ret = bignum_add(result, &a_abs, b);
        result->is_negative = 1;
        bignum_deinit(&a_abs);
        return ret;
    }

    comparison = _bignum_compare_abs(a, b);

    if (comparison == 0) {
        _bignum_set_zero(result);
        return CALC_OK;
    }

    /* a(-) - b(-) */
    if (a->is_negative && b->is_negative) {
        if (comparison > 0) {
            /* |a| > |b| -> -(a - b) */
            int ret = _bignum_sub_core(result, a, b);
            result->is_negative = 1;
            return ret;
        } else {
            /* |a| < |b| -> b - a */
            return _bignum_sub_core(result, b, a);
        }
    }

    /* a(+) - b(+) */
    if (comparison > 0) {
        return _bignum_sub_core(result, a, b);
    } else {
        int ret = _bignum_sub_core(result, b, a);
        result->is_negative = 1;
        return ret;
    }
}

enum CalcError bignum_mul(struct BigNum *result, const struct BigNum *a, const struct BigNum *b)
{
    size_t i, j;
    size_t max_result_size;
    struct BigNum temp_result;

    if (!result || !a || !b) {
        return CALC_INVALID_ARG;
    }

    max_result_size = a->size + b->size;
    if (bignum_init(&temp_result, max_result_size + 1) != EXIT_SUCCESS) {
        return CALC_MALLOC_FAIL;
    }
    temp_result.size = max_result_size;

    for (i = 0; i < a->size; i++)
    {
        unsigned int carry = 0; 
        for (j = 0; j < b->size; j++)
        {
            unsigned int temp_product = (unsigned int)a->bytes[i] * (unsigned int)b->bytes[j];
            unsigned int temp_sum = temp_product + temp_result.bytes[i + j] + carry;
            temp_result.bytes[i + j] = (unsigned char)(temp_sum % BIGNUM_BASE);
            carry = temp_sum / BIGNUM_BASE;
        }
        if (carry > 0) {
            temp_result.bytes[i + j] = (unsigned char)carry; 
        }
    }

    temp_result.is_negative = a->is_negative ^ b->is_negative;

    while (temp_result.size > 1 && temp_result.bytes[temp_result.size - 1] == 0) {
        temp_result.size--;
    }

    bignum_deinit(result);
    *result = temp_result;

    return CALC_OK;
}

enum CalcError bignum_div(struct BigNum *result, const struct BigNum *a, const struct BigNum *b)
{
    struct BigNum temp_remainder = DEFAULT_BIGNUM;
    struct BigNum temp_quotient = DEFAULT_BIGNUM;
    enum CalcError ret;

    if (!result || !a || !b) {
        return CALC_INVALID_ARG;
    }

    ret = _bignum_div_core(&temp_quotient, &temp_remainder, a, b);

    if (ret == CALC_OK) {
        bignum_deinit(result);
        *result = temp_quotient;
    } else {
        bignum_deinit(&temp_quotient);
    }
    bignum_deinit(&temp_remainder); 
    return ret;
}

enum CalcError bignum_mod(struct BigNum *result, const struct BigNum *a, const struct BigNum *b)
{
    struct BigNum temp_quotient = DEFAULT_BIGNUM;
    struct BigNum temp_remainder = DEFAULT_BIGNUM;
    enum CalcError ret;

    if (!result || !a || !b) {
        return CALC_INVALID_ARG;
    }

    ret = _bignum_div_core(&temp_quotient, &temp_remainder, a, b);

    if (ret == CALC_OK) {
        bignum_deinit(result);
        *result = temp_remainder;
    } else {
        bignum_deinit(&temp_remainder);
    }
    bignum_deinit(&temp_quotient);
    return ret;
}

enum CalcError bignum_negate(struct BigNum *result, const struct BigNum *a)
{
    struct BigNum temp;

    if (!result || !a) {
        return CALC_INVALID_ARG;
    }

    if (bignum_copy(&temp, a) != EXIT_SUCCESS) {
        return CALC_MALLOC_FAIL;
    }

    if (temp.size == 1 && temp.bytes[0] == 0) {
        temp.is_negative = 0;
    } else {
        temp.is_negative = !a->is_negative;
    }

    bignum_deinit(result);
    *result = temp;
    return CALC_OK;
}

enum CalcError bignum_factorial(struct BigNum *result, const struct BigNum *a)
{
    struct BigNum counter;
    enum CalcError ret = CALC_OK;
    int counter_inited = 0;

    if (!result || !a) {
        return CALC_INVALID_ARG;
    }

    if (a->is_negative) {
        return CALC_NEGATIVE_FACTORIAL;
    }

    if (_bignum_from_int(result, 1) != EXIT_SUCCESS) {
        return CALC_MALLOC_FAIL;
    }

    if (_bignum_is_zero(a) || (a->size == 1 && a->bytes[0] == 1)) {
        return CALC_OK;
    }

    if (bignum_copy(&counter, a) != EXIT_SUCCESS) {
        ret = CALC_MALLOC_FAIL; goto cleanup_fact;
    }
    counter_inited = 1;

    while ( !(counter.size == 1 && counter.bytes[0] <= 1) )
    {
        if (bignum_mul(result, result, &counter) != EXIT_SUCCESS) {
            ret = CALC_MALLOC_FAIL; goto cleanup_fact;
        }
        _bignum_dec_one(&counter);
    }

cleanup_fact:
    if (counter_inited) bignum_deinit(&counter);
    if (ret != EXIT_SUCCESS) bignum_deinit(result);
    return ret;
}

enum CalcError bignum_pow(struct BigNum *result, const struct BigNum *a, const struct BigNum *b)
{
    struct BigNum base_copy;
    struct BigNum exp_copy;
    enum CalcError ret = CALC_OK;
    int base_inited = 0, exp_inited = 0;

    if (!result || !a || !b) {
        return CALC_INVALID_ARG;
    }

    if (b->is_negative) {
        return CALC_NEGATIVE_EXPONENT;
    }

    if (_bignum_from_int(result, 1) != EXIT_SUCCESS) {
        return CALC_MALLOC_FAIL;
    }

    if (_bignum_is_zero(b)) {
        return CALC_OK;
    }

    if (bignum_copy(&base_copy, a) != EXIT_SUCCESS) {
        ret = CALC_MALLOC_FAIL;
        goto cleanup_pow;
    }
    base_inited = 1;
    if (bignum_copy(&exp_copy, b) != EXIT_SUCCESS) {
        ret = CALC_MALLOC_FAIL;
        goto cleanup_pow;
    }
    exp_inited = 1;

    /* Binární umocňování */
    while (!_bignum_is_zero(&exp_copy))
    {
        if (_bignum_is_odd(&exp_copy)) {
            if (bignum_mul(result, result, &base_copy) != EXIT_SUCCESS) {
                ret = EXIT_FAILURE; goto cleanup_pow;
            }
        }

        _bignum_div_by_small(&exp_copy, 2);
        
        if (!_bignum_is_zero(&exp_copy)) {
            if (bignum_mul(&base_copy, &base_copy, &base_copy) != EXIT_SUCCESS) {
                ret = CALC_MALLOC_FAIL; goto cleanup_pow;
            }
        }
    }

cleanup_pow:
    if (base_inited) bignum_deinit(&base_copy);
    if (exp_inited) bignum_deinit(&exp_copy);
    if (ret != EXIT_SUCCESS) bignum_deinit(result);
    return ret;
}