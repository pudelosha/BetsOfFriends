const fs = require('fs');
const packageJson = require('../package.json');
const version = packageJson.version;

const targetPath = './src/environments/version.ts';
const versionFileContent = `export const version = '${version}';\n`;

fs.writeFileSync(targetPath, versionFileContent, { encoding: 'utf8' });
console.log(`Version ${version} written to ${targetPath}`);
