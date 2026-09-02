import sqlite3
import sys

# use absolute path to database
db_path = r'C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\royal-d-debtor-web\RoyalD.Web\app.db'

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    cursor.execute('SELECT SalesRep FROM SalesBills LIMIT 5')
    rows = cursor.fetchall()
    print('SalesBills.SalesRep:')
    for row in rows:
        print(row[0])
    
    cursor.execute('SELECT CustomerName FROM OutstandingDebts LIMIT 5')
    rows = cursor.fetchall()
    print('\nOutstandingDebts.CustomerName:')
    for row in rows:
        print(row[0])
    
    conn.close()
except Exception as e:
    print(f'Error: {e}')
