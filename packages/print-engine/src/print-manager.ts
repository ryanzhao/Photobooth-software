import { PDFDocument } from 'pdf-lib';

export interface PrintArtifact {
  jpegBlob: Blob;
  pdfBlob: Blob;
}

function dataUrlToUint8Array(dataUrl: string): Uint8Array {
  const base64 = dataUrl.split(',')[1] ?? '';
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }
  return bytes;
}

export async function buildPrintArtifact(options: {
  dataUrl: string;
  width: number;
  height: number;
}): Promise<PrintArtifact> {
  const jpegBytes = dataUrlToUint8Array(options.dataUrl);
  const jpegBlob = new Blob([jpegBytes], { type: 'image/jpeg' });

  const pdf = await PDFDocument.create();
  const page = pdf.addPage([options.width, options.height]);
  const jpg = await pdf.embedJpg(jpegBytes);
  page.drawImage(jpg, {
    x: 0,
    y: 0,
    width: options.width,
    height: options.height
  });
  const pdfBlob = new Blob([await pdf.save()], { type: 'application/pdf' });

  return { jpegBlob, pdfBlob };
}

export async function openBrowserPrintPreview(dataUrl: string): Promise<void> {
  const printWindow = window.open('', '_blank', 'noopener,noreferrer,width=1280,height=900');
  if (!printWindow) {
    throw new Error('Unable to open print preview window.');
  }

  printWindow.document.write(`
    <html>
      <head>
        <title>Photobooth Print Preview</title>
        <style>
          html, body { margin: 0; padding: 0; background: #111827; display: grid; place-items: center; min-height: 100%; }
          img { max-width: 100vw; max-height: 100vh; object-fit: contain; }
        </style>
      </head>
      <body>
        <img src="${dataUrl}" alt="Print preview" />
        <script>
          window.addEventListener('load', () => setTimeout(() => window.print(), 150));
        </script>
      </body>
    </html>
  `);
  printWindow.document.close();
}
