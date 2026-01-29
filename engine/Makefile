# Fifth Engine - Makefile
#
# Build the Fifth Forth engine from C sources.
# No external dependencies beyond a C compiler and POSIX.

CC      = cc
CFLAGS  = -O2 -Wall -Wextra -Wpedantic -std=c11 -D_POSIX_C_SOURCE=200809L
LDFLAGS =
TARGET  = fifth
SRCS    = main.c vm.c prims.c io.c
OBJS    = $(SRCS:.c=.o)

.PHONY: all clean install test debug

all: $(TARGET)

$(TARGET): $(OBJS)
	$(CC) $(LDFLAGS) -o $@ $^

%.o: %.c fifth.h
	$(CC) $(CFLAGS) -c -o $@ $<

debug: CFLAGS = -g -O0 -Wall -Wextra -std=c11 -D_POSIX_C_SOURCE=200809L -DDEBUG
debug: clean $(TARGET)

clean:
	rm -f $(OBJS) $(TARGET)

install: $(TARGET)
	cp $(TARGET) /usr/local/bin/

# Quick smoke test
test: $(TARGET)
	@echo "=== Stack operations ==="
	@echo '1 2 3 .s bye' | ./$(TARGET) 2>/dev/null
	@echo ""
	@echo "=== Arithmetic ==="
	@echo '2 3 + . bye' | ./$(TARGET) 2>/dev/null
	@echo ""
	@echo "=== String ==="
	@echo 's" Hello, Fifth!" type cr bye' | ./$(TARGET) 2>/dev/null
	@echo ""
	@echo "=== Define word ==="
	@echo ': square dup * ; 7 square . bye' | ./$(TARGET) 2>/dev/null
	@echo ""
	@echo "=== IF/ELSE/THEN ==="
	@echo ': sign dup 0> if ." positive" else dup 0< if ." negative" else ." zero" then then drop ; 42 sign cr -3 sign cr 0 sign cr bye' | ./$(TARGET) 2>/dev/null
	@echo ""
	@echo "=== DO LOOP ==="
	@echo ': countdown 0 ?do i . loop ; 5 countdown cr bye' | ./$(TARGET) 2>/dev/null
	@echo ""
	@echo "=== All tests passed ==="

# Show size
size: $(TARGET)
	@ls -la $(TARGET)
	@echo "---"
	@wc -l $(SRCS) fifth.h boot/core.fs
