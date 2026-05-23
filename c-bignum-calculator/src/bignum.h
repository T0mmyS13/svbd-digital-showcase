#ifndef BIGNUM_H
#define BIGNUM_H

#include <stddef.h>

/**
 * \brief Struktura pro uložení libovolně velkého celého čísla.
 * Číslo je uloženo jako pole bajtů (little-endian).
 */
struct BigNum {
    unsigned char *bytes;  /* Dynamicky alokované pole bajtů */
    size_t capacity;       /* Počet alokovaných bajtů */
    size_t size;           /* Počet skutečně použitých bajtů */
    int is_negative;       /* 1 pokud je číslo záporné, 0 jinak */
};

/**
 * \brief Definice návratových kódů (chyb).
 */
enum CalcError {
    CALC_OK = 0,
    CALC_SYNTAX_ERROR,
    CALC_DIV_ZERO,
    CALC_NEGATIVE_FACTORIAL,
    CALC_NEGATIVE_EXPONENT,
    CALC_INVALID_COMMAND,
    CALC_MALLOC_FAIL,
    CALC_INVALID_ARG
};

#define DEFAULT_BIGNUM {NULL, 0, 0, 0}

#define BASE_DEC 10
#define BASE_BIN 2
#define BASE_HEX 16

/* === Správa paměti === */

/**
 * \brief Inicializuje existující instanci struktury BigNum.
 * \param n Ukazatel na strukturu BigNum.
 * \param capacity Počáteční kapacita v bajtech.
 * \return EXIT_SUCCESS při úspěchu, jinak EXIT_FAILURE.
 */
int bignum_init(struct BigNum *n, size_t capacity);

/**
 * \brief Uvolní paměť alokovanou pro bajty.
 * \param n Ukazatel na strukturu BigNum.
 */
void bignum_deinit(struct BigNum *n);

/**
 * \brief Vytvoří kopii čísla 'source' do 'dest'.
 * \param dest Cílová struktura BigNum (bude inicializována nebo přepsána).
 * \param source Zdrojová struktura BigNum.
 * \return EXIT_SUCCESS při úspěchu, jinak EXIT_FAILURE.
 */
int bignum_copy(struct BigNum *dest, const struct BigNum *source);


/* === Konverze === */

/**
 * \brief Převede řetězec (v bázi 10, 2, nebo 16) na BigNum.
 * \param n Cílová struktura BigNum.
 * \param str Vstupní řetězec.
 * \return EXIT_SUCCESS při úspěchu, jinak EXIT_FAILURE.
 */
int bignum_from_string(struct BigNum *n, const char *str);

/**
 * \brief Převede BigNum na řetězec ve zvolené bázi.
 * \param n Číslo k převodu.
 * \param base Číselná soustava (2, 10, 16).
 * \return Dynamicky alokovaný řetězec s výsledkem nebo NULL při chybě.
 */
char *bignum_to_string(const struct BigNum *n, int base);


/* === Aritmetické operace === */

/**
 * \brief Sečte dvě čísla: result = a + b.
 * \param result Ukazatel na výsledek.
 * \param a První sčítanec.
 * \param b Druhý sčítanec.
 * \return Kód chyby (CALC_OK při úspěchu).
 */
enum CalcError bignum_add(struct BigNum *result, const struct BigNum *a, const struct BigNum *b);

/**
 * \brief Odečte dvě čísla: result = a - b.
 * \param result Ukazatel na výsledek.
 * \param a Menšenec.
 * \param b Menšitel.
 * \return Kód chyby (CALC_OK při úspěchu).
 */
enum CalcError bignum_sub(struct BigNum *result, const struct BigNum *a, const struct BigNum *b);

/**
 * \brief Vynásobí dvě čísla: result = a * b.
 * \param result Ukazatel na výsledek.
 * \param a Činitel.
 * \param b Činitel.
 * \return Kód chyby (CALC_OK při úspěchu).
 */
enum CalcError bignum_mul(struct BigNum *result, const struct BigNum *a, const struct BigNum *b);

/**
 * \brief Vydělí dvě čísla (celočíselně): result = a / b.
 * \param result Ukazatel na výsledek.
 * \param a Dělenec.
 * \param b Dělitel.
 * \return Kód chyby (CALC_OK při úspěchu, CALC_DIV_ZERO při dělení nulou).
 */
enum CalcError bignum_div(struct BigNum *result, const struct BigNum *a, const struct BigNum *b);

/**
 * \brief Spočítá zbytek po dělení: result = a % b.
 * \param result Ukazatel na výsledek.
 * \param a Dělenec.
 * \param b Dělitel.
 * \return Kód chyby (CALC_OK při úspěchu, CALC_DIV_ZERO při dělení nulou).
 */
enum CalcError bignum_mod(struct BigNum *result, const struct BigNum *a, const struct BigNum *b);

/**
 * \brief Zneguje číslo: result = -a.
 * \param result Ukazatel na výsledek.
 * \param a Číslo k negaci.
 * \return Kód chyby (CALC_OK při úspěchu).
 */
enum CalcError bignum_negate(struct BigNum *result, const struct BigNum *a);

/**
 * \brief Spočítá faktoriál: result = a!
 * \param result Ukazatel na výsledek.
 * \param a Číslo, jehož faktoriál se má spočítat.
 * \return Kód chyby (CALC_OK při úspěchu, CALC_NEGATIVE_FACTORIAL pro záporný vstup).
 */
enum CalcError bignum_factorial(struct BigNum *result, const struct BigNum *a);

/**
 * \brief Umocní číslo: result = a ^ b.
 * \param result Ukazatel na výsledek.
 * \param a Základ mocniny.
 * \param b Exponent.
 * \return Kód chyby (CALC_OK při úspěchu, CALC_NEGATIVE_EXPONENT pro záporný exponent).
 */
enum CalcError bignum_pow(struct BigNum *result, const struct BigNum *a, const struct BigNum *b);

#endif