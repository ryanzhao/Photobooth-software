import { createScaffoldedProvider } from './provider-factory';

export const nikonProvider = createScaffoldedProvider({
  id: 'nikon-scaffold',
  label: 'Nikon Tethered Scaffold',
  diagnostics: ['Nikon provider scaffolded.', 'Integrate Nikon SDK or Windows bridge for production tethered control.']
});
