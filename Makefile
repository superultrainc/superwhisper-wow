.PHONY: test run
test:
	PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s companion
	lua5.1 tests/routes.lua
	lua5.1 tests/binding.lua
	lua5.1 tests/listening.lua

run:
	python3 companion/wow.py
