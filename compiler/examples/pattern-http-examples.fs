\ pattern_http_examples.fs - Pattern HTTP API Usage Examples

: header ( -- )
  ." === Fast Forth Pattern HTTP API Examples ===" cr cr ;

: example1 ( -- )
  ." Example 1: Health check" cr
  ." $ curl http://localhost:8080/health" cr cr ;

: example2 ( -- )
  ." Example 2: List all patterns" cr
  ." $ curl http://localhost:8080/patterns" cr cr ;

: example3 ( -- )
  ." Example 3: Get specific pattern" cr
  ." $ curl http://localhost:8080/patterns/DUP_TRANSFORM_001" cr cr ;

: example4 ( -- )
  ." Example 4: Query recursive patterns" cr
  ." $ curl -X POST http://localhost:8080/patterns/query \" cr
  ."   -H 'Content-Type: application/json' \" cr
  ."   -d '{" cr
  ."     \"category\": \"recursive\"," cr
  ."     \"limit\": 10" cr
  ."   }'" cr cr ;

: example5 ( -- )
  ." Example 5: Query O(1) patterns" cr
  ." $ curl -X POST http://localhost:8080/patterns/query \" cr
  ."   -H 'Content-Type: application/json' \" cr
  ."   -d '{" cr
  ."     \"performance_class\": \"O(1)\"," cr
  ."     \"limit\": 5" cr
  ."   }'" cr cr ;

: example6 ( -- )
  ." Example 6: Query patterns with 'factorial' tag" cr
  ." $ curl -X POST http://localhost:8080/patterns/query \" cr
  ."   -H 'Content-Type: application/json' \" cr
  ."   -d '{" cr
  ."     \"tags\": [\"factorial\"]" cr
  ."   }'" cr cr ;

: example7 ( -- )
  ." Example 7: Complex query with multiple filters" cr
  ." $ curl -X POST http://localhost:8080/patterns/query \" cr
  ."   -H 'Content-Type: application/json' \" cr
  ."   -d '{" cr
  ."     \"category\": \"recursive\"," cr
  ."     \"performance_class\": \"O(n)\"," cr
  ."     \"tags\": [\"optimized\"]," cr
  ."     \"limit\": 5," cr
  ."     \"offset\": 0" cr
  ."   }'" cr cr ;

: example8 ( -- )
  ." Example 8: List all pattern categories" cr
  ." $ curl http://localhost:8080/patterns/categories" cr cr ;

: example9 ( -- )
  ." Example 9: Find patterns with specific stack effect" cr
  ." $ curl -X POST http://localhost:8080/patterns/query \" cr
  ."   -H 'Content-Type: application/json' \" cr
  ."   -d '{" cr
  ."     \"stack_effect\": \"( n -- n! )\"" cr
  ."   }'" cr cr ;

: example10 ( -- )
  ." Example 10: Paginated query" cr
  ." $ curl -X POST http://localhost:8080/patterns/query \" cr
  ."   -H 'Content-Type: application/json' \" cr
  ."   -d '{" cr
  ."     \"limit\": 10," cr
  ."     \"offset\": 20" cr
  ."   }'" cr cr ;

: main ( -- )
  header
  example1 example2 example3 example4 example5
  example6 example7 example8 example9 example10 ;

main
bye
