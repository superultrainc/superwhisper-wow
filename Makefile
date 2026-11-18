.PHONY: test run
test:
	PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s companion

run:
	python3 companion/wow.py
