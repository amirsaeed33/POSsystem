const fs = require('fs');
const path = require('path');

function processDirectory(dir) {
    const files = fs.readdirSync(dir);
    for (const file of files) {
        const fullPath = path.join(dir, file);
        const stat = fs.statSync(fullPath);
        
        if (stat.isDirectory()) {
            processDirectory(fullPath);
        } else if (fullPath.endsWith('.html')) {
            let content = fs.readFileSync(fullPath, 'utf8');
            let originalContent = content;
            
            // Replace | date: 'mediumDate' or similar with | date: formatService.dateFormat
            content = content.replace(/\|\s*date\s*:\s*'[a-zA-Z0-9_\-]+'/g, "| date: formatService.dateFormat");
            // Replace | currency: 'PKR ' with formatService.currencyFormat
            content = content.replace(/\|\s*currency\s*:\s*'[^']+'/g, "| currency: formatService.currencyFormat");
            
            if (content !== originalContent) {
                fs.writeFileSync(fullPath, content);
                
                // Now find the corresponding .ts file and inject AppFormatService
                const tsPath = fullPath.replace('.html', '.ts');
                if (fs.existsSync(tsPath)) {
                    let tsContent = fs.readFileSync(tsPath, 'utf8');
                    let tsOriginal = tsContent;
                    
                    if (!tsContent.includes('AppFormatService')) {
                        tsContent = "import { AppFormatService } from 'src/app/demo/service/app-format.service';\n" + tsContent;
                    }
                    
                    // Add public formatService: AppFormatService to constructor
                    if (tsContent.includes('constructor(')) {
                        if (!tsContent.includes('public formatService: AppFormatService')) {
                            tsContent = tsContent.replace('constructor(', 'constructor(public formatService: AppFormatService, ');
                        }
                    } else {
                        // Create constructor
                        tsContent = tsContent.replace(/export class [^{]+{/, "$&\n    constructor(public formatService: AppFormatService) {}\n");
                    }
                    
                    if (tsContent !== tsOriginal) {
                        fs.writeFileSync(tsPath, tsContent);
                        console.log(`Updated ${tsPath}`);
                    }
                }
                console.log(`Updated ${fullPath}`);
            }
        }
    }
}

processDirectory('e:/Github/POSsystem/angular/src/app/demo/components');
