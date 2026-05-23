#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>

#include "stack/stack.h"
#include "bignum.h"
#include "parser.h"

#define MAX_LINE_LENGTH 1024
#define DEFAULT_OUTPUT_BASE 10
#define BIGNUM_INIT_CAPACITY 1

#define MODE_INTERACTIVE 1
#define MODE_FILE 0

#define ARGC_INTERACTIVE 1
#define ARGC_FILE 2

#define TRUE 1
#define FALSE 0

/**
 * \brief Uvolní paměť alokovanou pro výsledek, pokud byla inicializována.
 * 
 * \param result Ukazatel na strukturu BigNum, která se má uvolnit.
 * \param result_inited Ukazatel na příznak, zda byla struktura inicializována. Po uvolnění se nastaví na 0.
 * \return Nic (void).
 */
static void _cleanup_result(struct BigNum *result, int *result_inited)
{
    if (*result_inited) {
        bignum_deinit(result);
        *result_inited = 0;
    }
}

/**
 * \brief Porovná dva řetězce bez ohledu na velikost písmen (case-insensitive).
 * 
 * \param s1 První řetězec.
 * \param s2 Druhý řetězec.
 * \return Rozdíl mezi prvními odlišnými znaky (0 pokud jsou shodné).
 */
static int _strcmp(const char *s1, const char *s2)
{
    while (*s1 && (tolower((unsigned char)*s1) == tolower((unsigned char)*s2))) {
        s1++;
        s2++;
    }
    return tolower((unsigned char)*s1) - tolower((unsigned char)*s2);
}

/**
 * \brief Zkontroluje, zda řetězec vypadá jako příkaz (písmena, číslice a podtržítka, nezačíná číslicí).
 * 
 * \param s Řetězec ke kontrole.
 * \return 1 pokud řetězec vypadá jako příkaz, jinak 0.
 */
static int _is_command_string(const char *s)
{
    if (*s == '\0') {
        return FALSE;
    }
    if (isdigit((unsigned char)*s)) {
        return FALSE;
    }
    while (*s) {
        if (!isalnum((unsigned char)*s) && *s != '_') {
            return FALSE;
        }
        s++;
    }
    return TRUE;
}

/**
 * \brief Zpracovává vstupní příkazy a výrazy ze zadaného streamu.
 * 
 * \param stream Vstupní proud (stdin nebo soubor).
 * \param is_interactive_mode Příznak interaktivního režimu (1) nebo souborového režimu (0).
 * \return EXIT_SUCCESS při úspěšném ukončení, EXIT_FAILURE při chybě.
 */
static int _process_input(FILE *stream, int is_interactive_mode)
{
    char line[MAX_LINE_LENGTH];
    int current_output_base = DEFAULT_OUTPUT_BASE;

    while (1) 
    {
        size_t len;
        struct BigNum result;
        char *output_string = NULL;
        
        int result_inited = 0;
        if (bignum_init(&result, BIGNUM_INIT_CAPACITY) != EXIT_SUCCESS) {
            printf("Chyba: Nedostatek paměti pro spuštění!\n");
            return EXIT_FAILURE;
        }
        result_inited = 1;
        if (is_interactive_mode) {
            printf("> ");
            fflush(stdout);
        }

        if (fgets(line, MAX_LINE_LENGTH, stream) == NULL) {
            _cleanup_result(&result, &result_inited);
            break; 
        }
        
        if (!is_interactive_mode) {
            printf("> %s", line);
            len = strlen(line);
            if (len > 0 && line[len - 1] != '\n') {
                printf("\n");
            }
        }

        len = strlen(line);
        while (len > 0 && isspace((unsigned char)line[len - 1])) {
            line[--len] = '\0';
        }
        
        if (len == 0) {
            _cleanup_result(&result, &result_inited);
            continue; 
        }

        /* --- ZPRACOVÁNÍ PŘÍKAZŮ --- */
        if (_strcmp(line, "quit") == 0) {
            if (!is_interactive_mode) {
                printf("quit\n");
            }
            _cleanup_result(&result, &result_inited);
            break;
        }
        else if (_strcmp(line, "bin") == 0) {
            current_output_base = BASE_BIN;
            printf("bin\n");
            _cleanup_result(&result, &result_inited);
        }
        else if (_strcmp(line, "dec") == 0) {
            current_output_base = BASE_DEC;
            printf("dec\n");
            _cleanup_result(&result, &result_inited);
        }
        else if (_strcmp(line, "hex") == 0) {
            current_output_base = BASE_HEX;
            printf("hex\n");
            _cleanup_result(&result, &result_inited);
        }
        else if (_strcmp(line, "out") == 0) {
            if (current_output_base == BASE_BIN) {
                printf("bin\n");
            } else if (current_output_base == BASE_DEC) {
                printf("dec\n");
            } else {
                printf("hex\n");
            }
            _cleanup_result(&result, &result_inited);
        }
        /* --- ZPRACOVÁNÍ VÝRAZU --- */
        else 
        {
            enum CalcError error_code;

            _cleanup_result(&result, &result_inited);

            /* Pokud je zadán neznámý "příkaz" tvořený písmeny, číslicemi a podtržítky */
            if (_is_command_string(line) &&
                _strcmp(line, "quit") != 0 &&
                _strcmp(line, "bin")  != 0 &&
                _strcmp(line, "dec")  != 0 &&
                _strcmp(line, "hex")  != 0 &&
                _strcmp(line, "out")  != 0)
            {
                error_code = CALC_INVALID_COMMAND;
            }
            else
            {
                error_code = evaluate_expression(&result, line);
            }
            
            if (error_code == CALC_OK)
            {
                result_inited = 1;
                output_string = bignum_to_string(&result, current_output_base);
                
                if (output_string != NULL) {
                    printf("%s\n", output_string);
                } else {
                    printf("Chyba: bignum_to_string selhalo.\n");
                }
                free(output_string);
            }
            else 
            {
                switch (error_code)
                {
                    case CALC_SYNTAX_ERROR:
                        printf("Syntax error!\n");
                        break;
                    case CALC_DIV_ZERO:
                        printf("Division by zero!\n");
                        break;
                    case CALC_NEGATIVE_FACTORIAL:
                        printf("Input of factorial must not be negative!\n");
                        break;
                    case CALC_NEGATIVE_EXPONENT:
                        printf("Error: Negative exponent is not allowed.\n");
                        break;
                    case CALC_MALLOC_FAIL:
                        printf("Error: Out of memory.\n");
                        break;
                    case CALC_INVALID_ARG:
                        printf("Error: Invalid argument passed to function.\n");
                        break;
                    case CALC_INVALID_COMMAND:
                        printf("Invalid command \"%s\"!\n", line);
                        break;
                    default:
                        printf("Unknown error!\n");
                        break;
                }
            }
        }
        
        _cleanup_result(&result, &result_inited);
    }
    
    return EXIT_SUCCESS;
}

void run_interactive_mode(void)
{
    _process_input(stdin, MODE_INTERACTIVE);
}

int run_file_mode(char *filename)
{
    FILE *file;
    file = fopen(filename, "r");
    if (file == NULL)
    {
        printf("Invalid input file!\n");
        return EXIT_FAILURE;
    }

    _process_input(file, MODE_FILE);

    fclose(file);
    return EXIT_SUCCESS;
}

int main(int argc, char *argv[])
{
    if (argc == ARGC_INTERACTIVE)
    {
        run_interactive_mode();
        return EXIT_SUCCESS;
    }
    else if (argc == ARGC_FILE)
    {
        return run_file_mode(argv[1]);
    }
    else
    {
        printf("Usage: %s [<input-file>]\n", argv[0]);
        return EXIT_FAILURE;
    }
}