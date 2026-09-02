const fs = require('fs');
const Iconv = require('iconv-lite');

function fixMojibake(filePath) {
    const content = fs.readFileSync(filePath);
    // content is a Buffer of UTF-8 encoded text.
    // We want to decode it as utf8 to string,
    // then encode that string as Windows-874 (TIS-620) back to bytes,
    // then decode those bytes as utf-8.
    
    const str = content.toString('utf8');
    
    // Some characters might not be round-trippable. Let's do it char by char.
    let recovered = '';
    for(let i = 0; i < str.length; i++) {
        let c = str[i];
        if (c.charCodeAt(0) > 127) {
            try {
                // Encode to TIS-620
                let buf = Iconv.encode(c, 'win874');
                // Decode from UTF-8
                let dec = buf.toString('utf8');
                if (dec === '\uFFFD' || dec.length === 0) {
                    recovered += c; // fallback
                } else {
                    recovered += dec;
                }
            } catch(e) {
                recovered += c;
            }
        } else {
            recovered += c;
        }
    }
    
    fs.writeFileSync(filePath.replace('.cs', '_fixed.cs'), recovered, 'utf8');
    console.log(filePath + ' fixed');
}

fixMojibake('Services/ReportService.cs');
