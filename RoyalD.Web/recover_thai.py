def recover_text(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        text = f.read()
    try:
        # The text is UTF-8 encoded mojibake of cp1252 bytes
        # Let's try to encode to cp1252, which gives us the original UTF-8 bytes
        recovered_bytes = text.encode('cp1252')
        recovered_text = recovered_bytes.decode('utf-8')
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(recovered_text)
        print(f'Successfully recovered {filepath}')
    except Exception as e:
        print(f'Failed for {filepath}: {e}')

files = [
    'Views/Debtor/Index.cshtml',
    'Views/Debtor/History.cshtml',
    'Views/Debtor/Detail.cshtml',
    'Views/SalesBill/Detail.cshtml',
    'Views/SalesBill/Index.cshtml',
    'Controllers/DebtorController.cs',
    'Controllers/SalesBillController.cs',
    'Services/DebtorService.cs'
]
for file in files:
    recover_text(file)
