# Guia para hacer cambios en el proyecto


# 1. Git


Primero se debe crear una rama nueva a partir de la ultima version de develop


```bash
git switch develop
git pull origin develop
git switch -c feature/[nombre de feature]
```

# 2. Agente

Para poder hacer un uso optimo del agente, debemos hacerlo junto alguna skill que tome nuestro requerimiento y lo convierta en un plan de trabajo completo. 

El plugin `superpowers` trae un conjunto de skills para llevar a cabo la implementacion de una funcionalidad a traves de un conjunto de fases, buscando el mejor resultado posible para la menor cantidad de tokens.


## Instalacion 
```txt

# en claude

/plugin install superpowers@claude-plugins-official
/reload-plugins
```

```txt

# en opencode

Fetch and follow instructions from https://raw.githubusercontent.com/obra/superpowers/refs/heads/main/.opencode/INSTALL.md
```



## Uso

### Primer paso : Brainstorming

En este paso, la idea es definir el alcance de lo que se quiere hacer, el agente nos hara preguntas para lograrlo.

Uso:

```
/brainstorming [descripcion de feature/fix/bug]
```

Luego, la misma skill nos deberia de conducir a traves del resto de skills que se deben utilizar para ejecutar el flujo, es decir, una vez se termine la fase de brainstorming, la misma skill conduce al agente a utilizar la siguiente (`/writing-plans`)

Si lo anterior no ocurre, aca esta el resto de skills del plugin junto con una descripcion breve de la documentacion

```
/brainstorming - Activates before writing code. Refines rough ideas through questions, explores alternatives, presents design in sections for validation. Saves design document.

/using-git-worktrees - Activates after design approval. Creates isolated workspace on new branch, runs project setup, verifies clean test baseline.

/writing-plans - Activates with approved design. Breaks work into bite-sized tasks (2-5 minutes each). Every task has exact file paths, complete code, verification steps.

/subagent-driven-development or executing-plans - Activates with plan. Dispatches fresh subagent per task with two-stage review (spec compliance, then code quality), or executes in batches with human checkpoints.

/test-driven-development - Activates during implementation. Enforces RED-GREEN-REFACTOR: write failing test, watch it fail, write minimal code, watch it pass, commit. Deletes code written before tests.

/requesting-code-review - Activates between tasks. Reviews against plan, reports issues by severity. Critical issues block progress.

/finishing-a-development-branch - Activates when tasks complete. Verifies tests, presents options (merge/PR/keep/discard), cleans up worktree.
```

# Consideraciones de uso de agente

## 1. Reduccion de consumo de tokens

A menor consumo de tokens, mas avances en el proyecto


### 1.1 Caveman

Es una skill que comprime prompts a claude, indicandole lo mismo pero en muchisimas menos palabras.

**Instalacion**

```bash
claude plugin marketplace add JuliusBrussee/caveman
claude plugin install caveman@caveman
```

**Uso**

Al activarlo una vez en una sesion, queda activo para el resto de la sesion...


```
/caveman [lite|full|ultra]
```

Si se quiere detener ...

```
stop caveman
```

No se recomienda usar full o ultra por que podemos perder info importante.


### 1.2 code-review-graph

Crea un grafo de archivos del proyecto y hace que claude code consuma menos tokens por revision de codigo.

**Instalacion**

```bash
pipx install code-review-graph
code-review-graph instal --platform claude-code
code-review-graph build # construccion de grafo
code-review-graph status # revision de grafo
```

Para revisar cambios en el grafo con respecto al repo ...


**Actualizacion**

```bash
code-review-graph detect-changes --brief
```

Para actualizar grafo con respecto al repo ...

```bash
code-review-graph update --brief
```

**Uso**

Luego para hacer que claude code utilice el grafo en lugar de consumir directamente el codebase, se lo debemos pedir.

```
Antes de leer archivos directamente, usa code-review-graph para obtener el contexto mínimo afectado.
```

## 1.3 No dejar que las sesiones se expandan tanto

Por cada vez que se manda un mensaje, claude vuelve a leer toda la sesion, es bueno cambiar de sesion y no dejar que se alargen tanto.

Por cada vez que se quiera cambiar de sesion, pide un resumen a claude y copialo en la otra sesion.

## 2. Continuacion de sesiones


Cuando se termine la ventana de 5h durante la ejecucion de superpowers, usar la skill para continuar el workflow.

```
/resume-superpowers-workflow
```

# Git después de implementar la funcionalidad nueva

```bash
git branch backup/HU-X-before-squash
git reset --soft develop
git commit -m "xxx"
git log --oneline --graph --decorate --all
git checkout develop
git merge --ff-only feature/HU-X-slug
git push origin develop
git checkout feature/HU-X-slug
git push -u origin feature/HU-X-slug --force-with-lease
```
