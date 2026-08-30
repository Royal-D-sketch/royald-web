import sqlite3

conn = sqlite3.connect('royald.db')
cursor = conn.cursor()

cursor.execute('SELECT Id, BillNo, CustomerCode, CustomerName FROM OutstandingDebts WHERE Status != 0 LIMIT 10')
for row in cursor.fetchall():
    print(row)

conn.close()
