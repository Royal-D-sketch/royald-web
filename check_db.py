import sqlite3

conn = sqlite3.connect('royald.db')
cursor = conn.cursor()

print('--- Checking 420040 in OutstandingDebts ---')
cursor.execute("SELECT CustomerCode, CustomerName FROM OutstandingDebts WHERE CustomerCode = '420040' LIMIT 1")
print(cursor.fetchone())

print('--- Checking 420040 in Customers ---')
cursor.execute("SELECT Code, Name FROM Customers WHERE Code = '420040'")
print(cursor.fetchone())

print('--- Missing Names in Customers ---')
cursor.execute("SELECT COUNT(*) FROM Customers WHERE Name IS NULL OR Name = '' OR Name = '-'")
print(cursor.fetchone()[0])

print('--- Missing Names in Customers (List) ---')
cursor.execute("SELECT Code FROM Customers WHERE Name IS NULL OR Name = '' OR Name = '-' LIMIT 20")
print(cursor.fetchall())
