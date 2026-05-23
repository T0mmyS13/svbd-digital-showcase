#ifndef PARSER_H
#define PARSER_H

#include "bignum.h"

/* --- Datové typy pro Tokenizer a Shunting Yard --- */

/** \brief Typ tokenu ve výrazu */
enum TokenType {
    TOKEN_NUMBER,      /* Číslo (BigNum) */
    TOKEN_OPERATOR,    /* Operátor (+, -, *, !, ^, ~) */
    TOKEN_LEFT_PAREN,  /* Levá závorka ( */
    TOKEN_RIGHT_PAREN  /* Pravá závorka ) */
};

/** \brief Asociativita operátoru */
enum Associativity {
    ASSOC_LEFT,
    ASSOC_RIGHT
};

/**
 * \brief Definice vlastností operátoru.
 */
struct Operator {
    char op_char;       /* Znak operátoru */
    int precedence;     /* Priorita */
    enum Associativity assoc; /* Asociativita */
    int arity;          /* 1 = unární, 2 = binární */
};

/**
 * \brief Jeden token ve výrazu.
 */
struct Token {
    enum TokenType type;
    
    union {
        struct BigNum number;    
        struct Operator op;      
    } value;
};


/* --- Hlavní (veřejná) funkce modulu --- */

/**
 * \brief Hlavní funkce parseru.
 * Vezme řetězec v infixové notaci, spočítá ho a vrátí výsledek.
 * \param result Ukazatel na BigNum, kam se uloží výsledek.
 * \param expression Vstupní řetězec s matematickým výrazem.
 * \return Kód chyby (CALC_OK při úspěchu).
 */
enum CalcError evaluate_expression(struct BigNum *result, const char *expression);

#endif