
# Git

## Comando: partir desde `develop`

```bash
git switch develop
git pull origin develop
```


## Comando: crear rama feature

```bash
git switch -c feature/HU-X-slug
```

Ejemplo:

```bash
git switch -c feature/HU-01-crear-usuario-con-rol-inicial
```

### Revisar

Debes quedar trabajando en una rama con este formato:

```txt
feature/HU-X-slug
```

---

## Caso especial: si ya tienes cambios sin commit en `master`

Si los cambios pertenecen a una HU específica:

```bash
git stash push -u -m "wip HU-X antes de mover a feature"
git switch master
git switch -c develop
git switch -c feature/HU-X-slug
git stash pop
```

### Revisar

Los cambios deben quedar ahora dentro de la rama:

```txt
feature/HU-X-slug
```

Si los cambios son configuración base del proyecto, puedes hacer un commit base antes de empezar las HU:

```bash
git add .
git commit -m "chore: configurar estructura base del proyecto"
git switch -c develop
```

---


# Resumen uso de opencode

## Previo a implementacion

Lo primero que se debe hacer cuando se quiera implementar una HU-X es ...

```txt
/create-feature-sdd HU-X NOMBRE
/review-feature HU-X
/review-boundaries HU-X
/plan-feature HU-X
```


## Durante implementacion

```txt
/implement-task HU-X task N
/review-feature HU-X
/update-traceability HU-X
```

## Post implementacion

### Testing

**Backend**

```bash
dotnet test
```

**Front-end**

```bash
npm test
npm run lint
```

**Mobile**
```bash
npm test
npm run lint
```

### Cierre de la HU


```txt
/review-feature HU-X
```

### Revisar

Debe cumplirse:

```txt
- Todas las tasks están completas
- El SDD está actualizado
- Acceptance está actualizado
- Traceability está actualizado
- No hay blockers
- No hay TODOs bloqueantes
- No hay violaciones de boundaries
```

---

## Comando: trazabilidad final

```txt
/update-traceability HU-X
```

### Revisar

Solo marcar `Done` si:

```txt
- La implementación está completa
- Las pruebas pasan
- La revisión final no tiene blockers
- Acceptance tiene evidencia
- La HU no mezcla cambios de otra historia
```

---

# Git después de implementar la HU

```bash
git add .
git commit -m "feat(HU-X): nombre corto de la historia"
git push origin feature/HU-X-slug
```

# 10. Pull Request

## 10.1. Acción

Abrir PR:

```txt
feature/HU-X-slug → develop
```

# 11. Integración a develop

## 11.1. Opción recomendada

Usar PR hacia `develop`.

Si la rama ya tiene un solo commit, el merge normal es aceptable.

Si accidentalmente hay varios commits, usar:

```txt
Squash and merge
```
