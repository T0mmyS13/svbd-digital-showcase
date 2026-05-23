#include "parser.h"
#include "bignum.h"
#include "stack/stack.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>

#define TOKENIZER_INITIAL_CAPACITY 16

/* priorita operatoru */
#define PRECEDENCE_ULTRA_HIGH 5
#define PRECEDENCE_HIGHEST 4
#define PRECEDENCE_HIGH 3
#define PRECEDENCE_MEDIUM 2
#define PRECEDENCE_LOW 1

#define ARITY_UNARY 1
#define ARITY_BINARY 2

/* --- Definice operatoru --- */
/* poradi: znak, priorita, asociativita, arita */
static const struct Operator OPERATORS[] = {
    { '!', PRECEDENCE_ULTRA_HIGH, ASSOC_LEFT,  ARITY_UNARY },
    { '^', PRECEDENCE_HIGHEST, ASSOC_RIGHT, ARITY_BINARY },
    { '~', PRECEDENCE_HIGH,    ASSOC_RIGHT, ARITY_UNARY }, /* Unární mínus */
    { '*', PRECEDENCE_HIGH,    ASSOC_LEFT,  ARITY_BINARY },
    { '/', PRECEDENCE_HIGH,    ASSOC_LEFT,  ARITY_BINARY },
    { '%', PRECEDENCE_MEDIUM,  ASSOC_LEFT,  ARITY_BINARY },
    { '+', PRECEDENCE_LOW,     ASSOC_LEFT,  ARITY_BINARY },
    { '-', PRECEDENCE_LOW,     ASSOC_LEFT,  ARITY_BINARY }
};

/**
 * \brief Počet definovaných operátorů.
 */
static const size_t OPERATORS_COUNT = sizeof(OPERATORS) / sizeof(OPERATORS[0]);

/**
 * \brief Najde operátor v poli OPERATORS podle znaku.
 *
 * \param op_char Znak operátoru, který se má najít.
 * \return Ukazatel na strukturu Operator, nebo NULL pokud nebyl nalezen.
 */
static const struct Operator *_find_operator(char op_char)
{
    size_t i;
    for (i = 0; i < OPERATORS_COUNT; i++) {
        if (OPERATORS[i].op_char == op_char) {
            return &OPERATORS[i];
        }
    }
    return NULL;
}

/**
 * \brief Rozhodne, zda je znak '-' unární nebo binární operátor.
 *
 * \param last_token_type Typ předchozího tokenu.
 * \param last_op_char Znak posledního operátoru (pokud byl operátor).
 * \return 1 pokud je '-' unární, 0 pokud je binární.
 */
static int _is_unary_minus(int last_token_type, char last_op_char)
{
    if (last_token_type == -1) {
        return 1;
    }
    if (last_token_type == TOKEN_OPERATOR) {
        /* Po postfixovém operátoru (!) je '-' binární */
        if (last_op_char == '!') {
            return 0;
        }
        return 1;
    }
    if (last_token_type == TOKEN_LEFT_PAREN) {
        return 1;
    }
    return 0;
}

/**
 * \brief Dynamicky zvětší pole tokenů.
 * 
 * \param tokens Ukazatel na pole tokenů.
 * \param capacity Ukazatel na aktuální kapacitu pole, která bude aktualizována.
 * \return EXIT_SUCCESS při úspěchu, EXIT_FAILURE při chybě alokace.
 */
static int _resize_token_array(struct Token **tokens, size_t *capacity)
{
    size_t new_capacity = *capacity * 2;
    void *new_tokens = realloc(*tokens, new_capacity * sizeof(struct Token));
    if (!new_tokens) {
        return EXIT_FAILURE;
    }

    *tokens = (struct Token *)new_tokens;
    *capacity = new_capacity;
    return EXIT_SUCCESS;
}

/**
 * \brief Uvolní paměť pro BigNum v poli tokenů.
 *
 * \param tokens Pole tokenů k uvolnění.
 * \param count Počet tokenů v poli.
 * \return Nic (void).
 */
static void _free_tokens(struct Token *tokens, size_t count)
{
    size_t i;
    if (!tokens) {
        return;
    }
    
    for (i = 0; i < count; i++) {
        if (tokens[i].type == TOKEN_NUMBER) {
            bignum_deinit(&tokens[i].value.number);
        }
    }
    free(tokens);
}

/**
 * \brief Uklidí zásobník hodnot (value_stack).
 *
 * \param s Ukazatel na zásobník, který se má uvolnit.
 * \return Nic (void).
 */
static void _free_value_stack(struct stack *s)
{
    struct BigNum temp_num;
    if (!s) {
        return;
    }

    while (stack_item_count(s) > 0)
    {
        stack_pop(s, &temp_num);
        bignum_deinit(&temp_num);
    }
    stack_dealloc(&s);
}

/**
 * \brief Najde konec čísla ve výrazu.
 *
 * \param p Ukazatel na začátek čísla v řetězci.
 * \return Ukazatel na znak za koncem čísla.
 */
static const char *_find_number_end(const char *p)
{
    /* Hexadecimální (0x...) */
    if (p[0] == '0' && (p[1] == 'x' || p[1] == 'X')) {
        p += 2;
        while (isxdigit((unsigned char)*p)) p++;
        return p;
    }
    
    /* Binární (0b...) */
    if (p[0] == '0' && (p[1] == 'b' || p[1] == 'B')) {
        p += 2;
        while (*p == '0' || *p == '1') p++;
        return p;
    }
    
    /* Desítkové */
    while (isdigit((unsigned char)*p)) p++;
    return p;
}

/**
 * \brief Převede surový řetězec na pole tokenů.
 * 
 * \param tokens_out Výstupní parametr pro pole tokenů.
 * \param count_out Výstupní parametr pro počet tokenů.
 * \param expression Vstupní řetězec s výrazem.
 * \return CALC_OK při úspěchu, jinak chybový kód.
 */
static enum CalcError _tokenize(struct Token **tokens_out, size_t *count_out, const char *expression)
{
    const char *p = expression; 
    int last_token_type = -1;
    char last_op_char = '\0';
    size_t token_capacity = TOKENIZER_INITIAL_CAPACITY;
    size_t token_count = 0;
    struct Token *tokens = malloc(token_capacity * sizeof(struct Token));
    enum CalcError ret = CALC_OK;
    
    if (!tokens) {
        return CALC_MALLOC_FAIL;
    } 

    while (*p != '\0')
    {
        if (token_count >= token_capacity) {
            if (_resize_token_array(&tokens, &token_capacity) != EXIT_SUCCESS) {
                ret = CALC_MALLOC_FAIL;
                goto cleanup_tokenize;
            }
        }

        if (isspace((unsigned char)*p)) {
            p++;
            continue;
        }

        /* Zpracování čísla */
        if (isdigit((unsigned char)*p))
        {
            const char *end;
            size_t num_len;
            char *num_buffer;
            
            end = _find_number_end(p);
            num_len = (size_t)(end - p);
            
            num_buffer = (char *)malloc(num_len + 1);
            if (!num_buffer) {
                ret = CALC_MALLOC_FAIL;
                goto cleanup_tokenize;
            }
            memcpy(num_buffer, p, num_len);
            num_buffer[num_len] = '\0';
            
            tokens[token_count].type = TOKEN_NUMBER;
            if (bignum_from_string(&tokens[token_count].value.number, num_buffer) != EXIT_SUCCESS) {
                free(num_buffer);
                ret = CALC_SYNTAX_ERROR;
                goto cleanup_tokenize;
            }
            free(num_buffer);
            
            token_count++;
            last_token_type = TOKEN_NUMBER;
            p = end;
            continue;
        }
        
        /* Zpracování závorek */
        if (*p == '(') {
            tokens[token_count].type = TOKEN_LEFT_PAREN;
            token_count++;
            last_token_type = TOKEN_LEFT_PAREN;
            p++;
            continue;
        }
        if (*p == ')') {
            tokens[token_count].type = TOKEN_RIGHT_PAREN;
            token_count++;
            last_token_type = TOKEN_RIGHT_PAREN;
            p++;
            continue;
        }

        /* Zpracování operátorů */
        {
            char op_char_to_find = *p;
            const struct Operator *op = NULL;

            if (op_char_to_find == '-') {
                if (_is_unary_minus(last_token_type, last_op_char)) {
                    op_char_to_find = '~'; 
                }
            }
            
            op = _find_operator(op_char_to_find);
            
            if (op != NULL)
            {
                tokens[token_count].type = TOKEN_OPERATOR;
                tokens[token_count].value.op = *op;
                token_count++;
                last_token_type = TOKEN_OPERATOR;
                last_op_char = op->op_char;
                p++;
                continue;
            }
        }
        
        ret = CALC_SYNTAX_ERROR;
        goto cleanup_tokenize;
    }

cleanup_tokenize:
    if (ret == CALC_OK) {
        *tokens_out = tokens;
        *count_out = token_count;
    } else {
        _free_tokens(tokens, token_count);
        *tokens_out = NULL;
        *count_out = 0;
    }
    return ret;
}

/**
 * \brief SHUNTING YARD: Prevede infix na RPN.
 * 
 * \param rpn_tokens_out Vystupni parametr pro pole tokenu v RPN.
 * \param rpn_count_out Výstupní parametr pro počet tokenů v RPN.
 * \param tokens Vstupní pole tokenů (infix).
 * \param token_count Počet vstupních tokenů.
 * \return CALC_OK při úspěchu, jinak chybový kód.
 */
static enum CalcError _infix_to_rpn(struct Token **rpn_tokens_out, size_t *rpn_count_out,
                        const struct Token *tokens, size_t token_count)
{
    struct Token *rpn_tokens;
    size_t rpn_count = 0;
    struct stack *op_stack;
    enum CalcError ret = CALC_OK;
    size_t i;
    
    rpn_tokens = (struct Token *)malloc(token_count * sizeof(struct Token));
    if (!rpn_tokens) {
        return CALC_MALLOC_FAIL;
    }
    
    op_stack = stack_alloc(token_count, sizeof(struct Token));
    if (!op_stack) {
        free(rpn_tokens);
        return CALC_MALLOC_FAIL;
    }

    for (i = 0; i < token_count; i++)
    {
        const struct Token *token = &tokens[i];
        
        switch (token->type)
        {
            case TOKEN_NUMBER:
                rpn_tokens[rpn_count].type = TOKEN_NUMBER;
                if (bignum_copy(&rpn_tokens[rpn_count].value.number, &token->value.number) != EXIT_SUCCESS) {
                    ret = CALC_MALLOC_FAIL;
                    goto cleanup_rpn;
                }
                rpn_count++;
                break;
                
            case TOKEN_OPERATOR:
            {
                const struct Operator *op1 = &token->value.op;
                struct Token *top_token = (struct Token *)stack_head(op_stack);
                
                while (top_token != NULL && top_token->type == TOKEN_OPERATOR)
                {
                    const struct Operator *op2 = &top_token->value.op;
                    int should_pop = (op2->precedence > op1->precedence) ||
                                     (op2->precedence == op1->precedence && op1->assoc == ASSOC_LEFT);
                    
                    if (!should_pop) {
                        break;
                    }
                    
                    stack_pop(op_stack, &rpn_tokens[rpn_count]);
                    rpn_count++;
                    top_token = (struct Token *)stack_head(op_stack);
                }
                
                if (stack_push(op_stack, token) != EXIT_SUCCESS) {
                    ret = CALC_MALLOC_FAIL;
                    goto cleanup_rpn;
                }
                break;
            }
                
            case TOKEN_LEFT_PAREN:
                if (stack_push(op_stack, token) != EXIT_SUCCESS) { 
                    ret = CALC_MALLOC_FAIL;
                    goto cleanup_rpn;
                }
                break;
                
            case TOKEN_RIGHT_PAREN:
            {
                int found_left_paren = 0;
                while (stack_item_count(op_stack) > 0)
                {
                    struct Token *top_token = (struct Token *)stack_head(op_stack);
                    
                    if (top_token->type == TOKEN_LEFT_PAREN) {
                        stack_pop(op_stack, top_token);
                        found_left_paren = 1;
                        break;
                    }
                    stack_pop(op_stack, &rpn_tokens[rpn_count]);
                    rpn_count++;
                }
                
                if (!found_left_paren) {
                    ret = CALC_SYNTAX_ERROR;
                    goto cleanup_rpn;
                }
                break;
            }
        }
    }
    
    while (stack_item_count(op_stack) > 0)
    {
        struct Token *top_token = (struct Token *)stack_head(op_stack);
        
        if (top_token->type == TOKEN_LEFT_PAREN) {
            ret = CALC_SYNTAX_ERROR;
            goto cleanup_rpn;
        }
        stack_pop(op_stack, &rpn_tokens[rpn_count]);
        rpn_count++;
    }

cleanup_rpn:
    stack_dealloc(&op_stack);

    if (ret == CALC_OK) {
        *rpn_tokens_out = rpn_tokens;
        *rpn_count_out = rpn_count;
    } else {
        _free_tokens(rpn_tokens, rpn_count);
        *rpn_tokens_out = NULL;
        *rpn_count_out = 0;
    }
    return ret;
}


/**
 * \brief RPN EVALUÁTOR: Spočítá výsledek z RPN fronty.
 *
 * \param result Ukazatel na BigNum, kam se uloží výsledek.
 * \param rpn_tokens Pole tokenů v RPN.
 * \param rpn_count Počet tokenů v RPN.
 * \return CALC_OK při úspěchu, jinak chybový kód.
 */
static enum CalcError _rpn_evaluate(struct BigNum *result,
                        struct Token *rpn_tokens, size_t rpn_count)
{
    struct stack *value_stack;
    enum CalcError ret = CALC_OK;
    size_t i;

    struct BigNum a, b, temp_res = DEFAULT_BIGNUM;
    int a_inited = 0, b_inited = 0, temp_res_inited = 0;

    value_stack = stack_alloc(rpn_count, sizeof(struct BigNum));
    if (!value_stack) {
        return CALC_MALLOC_FAIL;
    }

    for (i = 0; i < rpn_count; i++)
    {
        struct Token *token = &rpn_tokens[i];
        
        if (token->type == TOKEN_NUMBER)
        {
            if (stack_push(value_stack, &token->value.number) != EXIT_SUCCESS) {
                ret = CALC_MALLOC_FAIL; goto cleanup_rpn_eval;
            }
            token->type = -1; /* Označíme jako "přesunuté" */
        }
        else if (token->type == TOKEN_OPERATOR)
        {
            const struct Operator *op = &token->value.op;
            temp_res_inited = 0;

            if (op->arity == 1) {
                if (stack_pop(value_stack, &a) != EXIT_SUCCESS) {
                    ret = CALC_SYNTAX_ERROR; goto cleanup_rpn_eval;
                }
                a_inited = 1; 

                if (op->op_char == '!') {
                    ret = bignum_factorial(&temp_res, &a);
                } else if (op->op_char == '~') {
                    ret = bignum_negate(&temp_res, &a);
                } else {
                    ret = CALC_SYNTAX_ERROR;
                }
                
                if (ret != CALC_OK) {
                    goto cleanup_rpn_eval;
                } 
                
                temp_res_inited = 1;
                if (stack_push(value_stack, &temp_res) != EXIT_SUCCESS) {
                    ret = CALC_MALLOC_FAIL; goto cleanup_rpn_eval;
                }
                temp_res_inited = 0; 
                temp_res.bytes = NULL;
                temp_res.capacity = 0;
                temp_res.size = 0;

                bignum_deinit(&a); a_inited = 0; 
            }
            else 
            {
                if (stack_pop(value_stack, &b) != EXIT_SUCCESS) {
                    ret = CALC_SYNTAX_ERROR; goto cleanup_rpn_eval;
                }
                b_inited = 1;
                if (stack_pop(value_stack, &a) != EXIT_SUCCESS) {
                    ret = CALC_SYNTAX_ERROR; goto cleanup_rpn_eval;
                }
                a_inited = 1; 
                
                if (op->op_char == '+') {
                    ret = bignum_add(&temp_res, &a, &b);
                } else if (op->op_char == '-') {
                    ret = bignum_sub(&temp_res, &a, &b);
                } else if (op->op_char == '*') {
                    ret = bignum_mul(&temp_res, &a, &b);
                } else if (op->op_char == '/') {
                    ret = bignum_div(&temp_res, &a, &b);
                } else if (op->op_char == '%') {
                    ret = bignum_mod(&temp_res, &a, &b);
                } else if (op->op_char == '^') {
                    ret = bignum_pow(&temp_res, &a, &b);
                } else {
                    ret = CALC_SYNTAX_ERROR;
                }
                
                if (ret != CALC_OK) {
                    goto cleanup_rpn_eval;
                } 
                
                temp_res_inited = 1;
                if (stack_push(value_stack, &temp_res) != EXIT_SUCCESS) {
                    ret = CALC_MALLOC_FAIL; goto cleanup_rpn_eval;
                }
                temp_res_inited = 0; 
                temp_res.bytes = NULL;
                temp_res.capacity = 0;
                temp_res.size = 0;

                bignum_deinit(&a); a_inited = 0;
                bignum_deinit(&b); b_inited = 0;
            }
        }
    } 

    if (stack_item_count(value_stack) != 1) {
        ret = CALC_SYNTAX_ERROR; 
        goto cleanup_rpn_eval;
    }

    stack_pop(value_stack, result);

cleanup_rpn_eval:
    if (a_inited) {
        bignum_deinit(&a);
    }
    if (b_inited) {
        bignum_deinit(&b);
    }
    if (temp_res_inited) {
        bignum_deinit(&temp_res);
    }
    _free_value_stack(value_stack); 
    return ret;
}

enum CalcError evaluate_expression(struct BigNum *result, const char *expression)
{
    struct Token *tokens = NULL;
    size_t token_count = 0;
    struct Token *rpn_tokens = NULL;
    size_t rpn_count = 0;
    enum CalcError ret = CALC_OK;

    if (!result || !expression) {
        return CALC_INVALID_ARG;
    }

    /* Fáze 1: Tokenizace */
    ret = _tokenize(&tokens, &token_count, expression);
    if (ret != CALC_OK) {
        return ret;
    }

    /* Fáze 2: Shunting Yard */
    ret = _infix_to_rpn(&rpn_tokens, &rpn_count, tokens, token_count);
    if (ret != CALC_OK) {
        goto cleanup_evaluate;
    }
    
    _free_tokens(tokens, token_count);
    tokens = NULL;

    /* Fáze 3: RPN Evaluátor */
    ret = _rpn_evaluate(result, rpn_tokens, rpn_count);
    if (ret != CALC_OK) {
        goto cleanup_evaluate;
    }

cleanup_evaluate:
    if (tokens) {
        _free_tokens(tokens, token_count);
    }
    _free_tokens(rpn_tokens, rpn_count);
    return ret;
}