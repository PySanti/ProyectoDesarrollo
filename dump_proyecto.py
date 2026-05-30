import os

def dumpear_proyecto(ruta_proyecto, archivo_salida):
    # Carpetas que ignoramos para no saturar el archivo (muy común en proyectos de desarrollo)
    ignorar_carpetas = {
        '.git', 'node_modules', '__pycache__', '.venv', 'venv', 'env', 
        '.idea', '.vscode', 'dist', 'build', '.next', '.nuxt', 'target'
    }
    # Archivos sueltos que no queremos incluir
    ignorar_archivos = {'.DS_Store', 'Thumbs.db', archivo_salida}
    
    # Extensiones binarias o multimedia que no se pueden leer como texto plano
    extensiones_binarias = {
        '.png', '.jpg', '.jpeg', '.gif', '.ico', '.pdf', '.zip', '.tar', '.gz', 
        '.exe', '.dll', '.so', '.pyc', '.mp3', '.mp4', '.wav', '.woff', '.woff2', '.ttf'
    }

    print(f"Iniciando el volcado (dump) de: {os.path.abspath(ruta_proyecto)}")
    
    with open(archivo_salida, 'w', encoding='utf-8', errors='replace') as f_salida:
        f_salida.write(f"============================================================")
        f_salida.write(f"DUMP COMPLETO DEL PROYECTO: {os.path.basename(os.path.abspath(ruta_proyecto))}")
        f_salida.write(f"============================================================")

        # 1. GENERAR EL ÁRBOL DE ESTRUCTURA
        f_salida.write("--- ESTRUCTURA DE DIRECTORIOS Y ARCHIVOS ---")
        for raiz, directorios, archivos in os.walk(ruta_proyecto):
            # Filtrar carpetas ignoradas para que no se recorran
            directorios[:] = [d for d in directorios if d not in ignorar_carpetas]
            
            nivel = raiz.replace(ruta_proyecto, '').count(os.sep)
            sangria = '  ' * nivel
            f_salida.write(f"{sangria}[D] {os.path.basename(raiz)}/")
            
            for archivo in archivos:
                if archivo not in ignorar_archivos:
                    _, ext = os.path.splitext(archivo)
                    if ext.lower() not in extensiones_binarias:
                        f_salida.write(f"{sangria}  [F] {archivo}")
        
        f_salida.write("" + "="*60 + "")
        f_salida.write("--- CONTENIDO DETALLADO DE ARCHIVOS ---")
        f_salida.write("="*60 + "")

        # 2. VOLCAR EL CONTENIDO DE CADA ARCHIVO
        for raiz, directorios, archivos in os.walk(ruta_proyecto):
            directorios[:] = [d for d in directorios if d not in ignorar_carpetas]
            
            for archivo in archivos:
                if archivo in ignorar_archivos:
                    continue
                    
                _, ext = os.path.splitext(archivo)
                if ext.lower() in extensiones_binarias:
                    continue
                    
                ruta_completa = os.path.join(raiz, archivo)
                ruta_relativa = os.path.relpath(ruta_completa, ruta_proyecto)
                
                f_salida.write(f"============================================================")
                f_salida.write(f"ARCHIVO: {ruta_relativa}")
                f_salida.write(f"============================================================")
                
                try:
                    with open(ruta_completa, 'r', encoding='utf-8', errors='replace') as f_entrada:
                        contenido = f_entrada.read()
                        f_salida.write(contenido)
                        if contenido and not contenido.endswith(''):
                            f_salida.write('')
                except Exception as e:
                    f_salida.write(f"[ERROR AL LEER ARCHIVO]: No se pudo leer el archivo. Motivo: {str(e)}")
                
                f_salida.write("")

    print(f"¡Proceso completado! Todo el contenido se ha guardado en: {archivo_salida}")

if __name__ == "__main__":
    # Puedes cambiar "." por la ruta absoluta de tu proyecto si no ejecutas el script dentro de él
    ruta_origen = "." 
    archivo_destino = "proyecto_completo_dump.txt"
    
    dumpear_proyecto(ruta_origen, archivo_destino)
