const LOGO_UTS_URL = 'assets/images/logos/uts-logo-color.png';
const LOGO_UTS_MAX_WIDTH = 480;

let logoUtsEnBase64: Promise<string | null> | undefined;

/** Carga una versión reducida del logo para no aumentar innecesariamente los PDF. */
export function obtenerLogoUtsPdf(): Promise<string | null> {
  if (logoUtsEnBase64) return logoUtsEnBase64;

  logoUtsEnBase64 = new Promise(resolve => {
    const imagen = new Image();

    imagen.onload = () => {
      if (!imagen.naturalWidth || !imagen.naturalHeight) {
        resolve(null);
        return;
      }

      const escala = Math.min(1, LOGO_UTS_MAX_WIDTH / imagen.naturalWidth);
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(imagen.naturalWidth * escala);
      canvas.height = Math.round(imagen.naturalHeight * escala);

      const contexto = canvas.getContext('2d');
      if (!contexto) {
        resolve(null);
        return;
      }

      contexto.drawImage(imagen, 0, 0, canvas.width, canvas.height);
      resolve(canvas.toDataURL('image/png'));
    };

    imagen.onerror = () => resolve(null);
    imagen.src = LOGO_UTS_URL;
  });

  return logoUtsEnBase64;
}

export function agregarLogoUtsPdf(
  documento: any,
  logo: string | null,
  x: number,
  y: number,
  ancho: number
): void {
  if (!logo) return;

  const propiedades = documento.getImageProperties(logo);
  const alto = ancho * (propiedades.height / propiedades.width);
  documento.addImage(logo, 'PNG', x, y, ancho, alto);
}
