import { createScaffoldedProvider } from './provider-factory';

export const canonProvider = createScaffoldedProvider({
  id: 'canon-scaffold',
  label: 'Canon Tethered Scaffold',
  diagnostics: ['Canon provider scaffolded.', 'Integrate Canon EDSDK bridge for production tethered control.']
});
