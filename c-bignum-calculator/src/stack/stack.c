#include "stack.h"
#include <stdlib.h>
#include <string.h>

int stack_init(struct stack *s, const size_t capacity, const size_t item_size)
{
    if (!s || capacity == 0 || item_size == 0) {
        return EXIT_FAILURE;
    }

    s->capacity = capacity;
    s->item_size = item_size;
    s->sp = 0;
    s->items = malloc(capacity * item_size);
    if (!s->items) {
        return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}

struct stack *stack_alloc(const size_t capacity, const size_t item_size)
{
    struct stack *new_stack;

    new_stack = malloc(sizeof(struct stack));
    if (!new_stack) {
        return NULL;
    }

    if (stack_init(new_stack, capacity, item_size) != EXIT_SUCCESS) {
        free(new_stack);
        return NULL;
    }
    return new_stack;
}

void stack_deinit(struct stack *s)
{
    if (s) {
        free(s->items);
        s->items = NULL;
        s->capacity = 0;
        s->item_size = 0;
        s->sp = 0;
    }
}

void stack_dealloc(struct stack **s)
{
    if (s && *s) {
        stack_deinit(*s);
        free(*s);
        *s = NULL;
    }
}

size_t stack_item_count(const struct stack *s)
{
    if (!s) {
        return 0;
    }
    return s->sp;
}

/**
 * \brief Interní pomocná funkce pro získání adresy prvku na daném indexu.
 * 
 * \param s Ukazatel na zásobník.
 * \param at Index prvku (0-based).
 * \return Ukazatel na paměť, kde je prvek uložen (void*).
 */
static void *_stack_at(const struct stack *s, const size_t at)
{
    return (char *)s->items + at * s->item_size;
}

int stack_push(struct stack *s, const void *item)
{
    if (!s || !item || s->sp >= s->capacity) {
        return EXIT_FAILURE;
    }

    memcpy(_stack_at(s, s->sp), item, s->item_size);
    s->sp++;
    return EXIT_SUCCESS;
}

void *stack_head(const struct stack *s)
{
    if (stack_item_count(s) == 0) {
        return NULL;
    }
    return _stack_at(s, s->sp - 1);
}

int stack_pop(struct stack *s, void *item)
{
    if (!s || !item || stack_item_count(s) == 0) {
        return EXIT_FAILURE;
    }

    memcpy(item, stack_head(s), s->item_size);
    s->sp--;
    return EXIT_SUCCESS;
}