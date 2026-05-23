CC = gcc

CFLAGS = -Wall -Wextra -pedantic -ansi -g
LDFLAGS = $(CFLAGS)

BUILD_DIR = build
BIN = calc.exe

all: clean $(BUILD_DIR) $(BIN)

$(BIN): $(BUILD_DIR)/main.o $(BUILD_DIR)/bignum.o $(BUILD_DIR)/parser.o $(BUILD_DIR)/stack.o
	$(CC) -o $@ $^ $(LDFLAGS)

$(BUILD_DIR)/main.o: src/main.c
	$(CC) -c $(CFLAGS) -o $@ $<

$(BUILD_DIR)/bignum.o: src/bignum.c
	$(CC) -c $(CFLAGS) -o $@ $<

$(BUILD_DIR)/parser.o: src/parser.c
	$(CC) -c $(CFLAGS) -o $@ $<

$(BUILD_DIR)/stack.o: src/stack/stack.c
	$(CC) -c $(CFLAGS) -o $@ $<

$(BUILD_DIR):
	mkdir $@

clean:
	rm -rf $(BUILD_DIR) $(BIN)

valgrind: all
	valgrind --leak-check=full --show-leak-kinds=all --track-origins=yes ./$(BIN) < test_input.txt