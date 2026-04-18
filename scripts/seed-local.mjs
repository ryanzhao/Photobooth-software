import { mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

const root = process.cwd();
const directories = [
  join(root, 'booth-data'),
  join(root, 'booth-data', 'sessions'),
  join(root, 'booth-data', 'exports')
];

for (const directory of directories) {
  await mkdir(directory, { recursive: true });
}

await writeFile(
  join(root, 'booth-data', 'README.txt'),
  [
    'This directory holds local-first booth data.',
    'Sessions will create originals/, processed/, and outputs/ subfolders under booth-data/sessions.'
  ].join('\n'),
  'utf8'
);

console.log('Local booth-data folders are ready.');
