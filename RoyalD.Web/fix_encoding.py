import sys

def fix_mojibake(input_file, output_file):
    with open(input_file, 'r', encoding='utf-8') as f:
        text = f.read()
    
    # Try to recover: utf-8 -> cp874 -> utf-8
    # Wait: The file was UTF-8. The bytes representing 'เธก' are 0xE0 0xB8 0xA1.
    # We want to treat 0xE0 0xB8 0xA1 as CP874 bytes, and then decode them as UTF-8.
    
    # Actually, the string in Python is 'เธก'. 
    # encode('cp874') gives bytes: b'\xe0\xb8\xa1'
    # decode('utf-8') gives string: 'ม'
    
    recovered_text = ''
    for char in text:
        try:
            # If the char is in Thai range (e.g. เ, ธ, ก), it can be encoded to cp874
            # If it's a normal ASCII char, it encodes to itself
            # We encode character by character or as whole text?
            pass
        except:
            pass

    try:
        # Encode whole text to CP874 bytes
        # Some characters might not be in CP874 (e.g. standard UTF-8 quotes?)
        # Let's use errors='ignore' or 'replace' for problematic ones, 
        # but wait, if it's pure ASCII + Mojibake, encode('cp874') will work perfectly.
        bytes_val = text.encode('cp874')
        recovered_text = bytes_val.decode('utf-8')
    except Exception as e:
        print(f'Error encoding/decoding: {e}')
        # Fallback to character by character if there are weird chars
        recovered_text = ''
        for char in text:
            try:
                b = char.encode('cp874')
                recovered_text += b.decode('utf-8')
            except:
                recovered_text += char
                
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(recovered_text)
    
    print('Done.')

fix_mojibake('Services/ReportService.cs', 'Services/ReportService_fixed.cs')
