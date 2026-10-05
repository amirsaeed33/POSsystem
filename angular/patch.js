const fs = require('fs');

let tsFile = 'e:/Github/POSsystem/angular/src/app/demo/components/online-order/online-order.component.ts';
let htmlFile = 'e:/Github/POSsystem/angular/src/app/demo/components/online-order/online-order.component.html';

// 1. Update TS
let tsContent = fs.readFileSync(tsFile, 'utf8');

// replace variables
tsContent = tsContent.replace("branchName = '';", "branchName = '';\n    currencyFormat = 'PKR';\n    dateFormat = 'dd/MM/yyyy';");

// replace resolveBranchName logic
tsContent = tsContent.replace(
    "if (info && info.branchName) {\n                this.branchName = info.branchName;\n                return;\n            }",
    "if (info && info.branchName) {\n                this.branchName = info.branchName;\n                this.currencyFormat = info.currencyFormat || 'PKR';\n                this.dateFormat = info.dateFormat || 'dd/MM/yyyy';\n                return;\n            }"
);
// replace fallback logic in resolveBranchName
tsContent = tsContent.replace(
    "this.branchName = '';\n    }",
    "this.branchName = '';\n        this.currencyFormat = 'PKR';\n        this.dateFormat = 'dd/MM/yyyy';\n    }"
);

fs.writeFileSync(tsFile, tsContent);

// 2. Update HTML
let htmlContent = fs.readFileSync(htmlFile, 'utf8');
htmlContent = htmlContent.replace(/formatService\.currencyFormat/g, 'currencyFormat');
htmlContent = htmlContent.replace(/formatService\.dateFormat/g, 'dateFormat');
fs.writeFileSync(htmlFile, htmlContent);

console.log("Done");
