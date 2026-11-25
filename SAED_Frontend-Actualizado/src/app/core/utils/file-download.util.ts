interface ArchivoEscribible {
  write(data: Blob): Promise<void>;
  close(): Promise<void>;
}

interface DestinoArchivo {
  createWritable(): Promise<ArchivoEscribible>;
}

interface ResultadoGuardadoNativo {
  cancelado: boolean;
  destino?: DestinoArchivo;
}

type SelectorArchivo = (options: {
  suggestedName: string;
  types: Array<{
    description: string;
    accept: Record<string, string[]>;
  }>;
}) => Promise<DestinoArchivo>;

export interface DescargaPreparada {
  usarGuardadoNativo: boolean;
  resultado?: Promise<ResultadoGuardadoNativo>;
}

/**
 * Solicita el destino mientras sigue vigente la interacción del usuario.
 * Chromium guarda el archivo de forma nativa, sin depender del nombre de una URL blob.
 */
export function prepararDescargaArchivo(nombreArchivo: string): DescargaPreparada {
  const selector = (window as Window & {
    showSaveFilePicker?: SelectorArchivo;
  }).showSaveFilePicker;

  if (!window.isSecureContext || !selector) {
    return { usarGuardadoNativo: false };
  }

  const extension = nombreArchivo.toLowerCase().endsWith('.zip') ? '.zip' : '.pdf';
  const mimeType = extension === '.zip' ? 'application/zip' : 'application/pdf';
  const descripcion = extension === '.zip' ? 'Archivo ZIP' : 'Documento PDF';

  try {
    const resultado = selector.call(window, {
      suggestedName: nombreArchivo,
      types: [{
        description: descripcion,
        accept: { [mimeType]: [extension] }
      }]
    }).then(
      destino => ({ cancelado: false, destino }),
      error => {
        if (error instanceof DOMException && error.name === 'AbortError') {
          return { cancelado: true };
        }

        throw error;
      }
    );

    return { usarGuardadoNativo: true, resultado };
  } catch {
    return { usarGuardadoNativo: false };
  }
}

export async function descargarArchivo(
  blob: Blob,
  nombreArchivo: string,
  descargaPreparada?: DescargaPreparada
): Promise<boolean> {
  if (descargaPreparada?.usarGuardadoNativo && descargaPreparada.resultado) {
    const resultado = await descargaPreparada.resultado;
    if (resultado.cancelado || !resultado.destino) {
      return false;
    }

    const archivo = await resultado.destino.createWritable();
    await archivo.write(blob);
    await archivo.close();
    return true;
  }

  const url = URL.createObjectURL(blob);
  const enlace = document.createElement('a');

  enlace.href = url;
  enlace.download = nombreArchivo;
  enlace.style.display = 'none';
  document.body.appendChild(enlace);
  enlace.click();
  enlace.remove();

  window.setTimeout(() => URL.revokeObjectURL(url), 1_000);
  return true;
}
