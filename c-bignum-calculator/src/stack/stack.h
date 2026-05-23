#ifndef STACK_H
#define STACK_H

#include <stddef.h>

/* Definice zásobníkové struktury */
struct stack {
    size_t capacity;    /* Maximální počet prvků */
    size_t item_size;   /* Velikost jednoho prvku */
    size_t sp;          /* Index vrcholu */
    void *items;        /* Ukazatel na paměť pro prvky */
};

#define DEFAULT_STACK {0, 0, 0, NULL}

/**
 * \brief Dynamicky alokuje a inicializuje novou instanci zásobníku.
 * \param capacity Počáteční kapacita zásobníku (počet prvků).
 * \param item_size Velikost jednoho prvku v bajtech.
 * \return Ukazatel na nově vytvořený zásobník nebo NULL při chybě alokace.
 */
struct stack *stack_alloc(const size_t capacity, const size_t item_size);

/**
 * \brief Inicializuje existující instanci struktury stack.
 * \param s Ukazatel na strukturu zásobníku.
 * \param capacity Počáteční kapacita zásobníku.
 * \param item_size Velikost jednoho prvku v bajtech.
 * \return EXIT_SUCCESS při úspěchu, jinak EXIT_FAILURE.
 */
int stack_init(struct stack *s, const size_t capacity, const size_t item_size);

/**
 * \brief Uvolní paměť alokovanou pro prvky zásobníku.
 * \param s Ukazatel na zásobník.
 */
void stack_deinit(struct stack *s);

/**
 * \brief Uvolní paměť pro prvky a poté i samotnou strukturu zásobníku.
 * \param s Ukazatel na ukazatel na zásobník (bude nastaven na NULL).
 */
void stack_dealloc(struct stack **s);

/**
 * \brief Vrátí aktuální počet prvků v zásobníku.
 * \param s Ukazatel na zásobník.
 * \return Počet prvků v zásobníku.
 */
size_t stack_item_count(const struct stack *s);

/**
 * \brief Vloží nový prvek na vrchol zásobníku.
 * \param s Ukazatel na zásobník.
 * \param item Ukazatel na data prvku, která se mají zkopírovat.
 * \return EXIT_SUCCESS při úspěchu, jinak EXIT_FAILURE.
 */
int stack_push(struct stack *s, const void *item);

/**
 * \brief Vrátí ukazatel na prvek na vrcholu zásobníku (neodebírá ho).
 * \param s Ukazatel na zásobník.
 * \return Ukazatel na data na vrcholu nebo NULL, pokud je zásobník prázdný.
 */
void *stack_head(const struct stack *s);

/**
 * \brief Odebere prvek z vrcholu zásobníku.
 * \param s Ukazatel na zásobník.
 * \param item Ukazatel na paměť, kam se má prvek zkopírovat.
 * \return EXIT_SUCCESS při úspěchu, jinak EXIT_FAILURE.
 */
int stack_pop(struct stack *s, void *item);

#endif