
# Git

## Estado de migracion documental

La doctrina documental cambio antes de la migracion completa del codigo. Antes de iniciar una rama o usar agentes, lee `docs/02-project-context/documentation-migration-status.md`.

Doctrina actual: servicios objetivo `Identity`, `Partidas`, `Operaciones de Sesion` y `Puntuaciones`, detras del gateway YARP obligatorio. Las carpetas de implementacion antiguas pueden seguir existiendo como deuda de migracion y no deben usarse como doctrina activa.

## Comando: partir desde `develop`

```bash
git switch develop
git pull origin develop
git switch -c feature/HU-X-slug
```

# Uso de plugin superpowers para implementacion de Funcionalidad

## Instalacion 
```
/plugin install superpowers@claude-plugins-official
/reload-plugins
```

## Uso

```
/brainstorming - Activates before writing code. Refines rough ideas through questions, explores alternatives, presents design in sections for validation. Saves design document.

/using-git-worktrees - Activates after design approval. Creates isolated workspace on new branch, runs project setup, verifies clean test baseline.

/writing-plans - Activates with approved design. Breaks work into bite-sized tasks (2-5 minutes each). Every task has exact file paths, complete code, verification steps.

/subagent-driven-development or executing-plans - Activates with plan. Dispatches fresh subagent per task with two-stage review (spec compliance, then code quality), or executes in batches with human checkpoints.

/test-driven-development - Activates during implementation. Enforces RED-GREEN-REFACTOR: write failing test, watch it fail, write minimal code, watch it pass, commit. Deletes code written before tests.

/requesting-code-review - Activates between tasks. Reviews against plan, reports issues by severity. Critical issues block progress.

/finishing-a-development-branch - Activates when tasks complete. Verifies tests, presents options (merge/PR/keep/discard), cleans up worktree.
```

# Git después de implementar la HU

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
