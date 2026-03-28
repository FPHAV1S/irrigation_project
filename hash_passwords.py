import bcrypt

## Скрипт за хеширане на пароли. Може да се добавят нови пароли също така. Важно е да се отбележи, че bcrypt генерира различен хеш всеки път, дори за една и съща парола, поради използването на salting. Това прави хешовете по-сигурни срещу атаки с wordlist и bruteforcing. Не е нещо много важно за поливна система, но е интересно да се има :D

testtest_password = "1203"

testtest_hash = bcrypt.hashpw(testtest_password.encode(), bcrypt.gensalt()).decode()

print(f"Test hash: {testtest_hash}")

