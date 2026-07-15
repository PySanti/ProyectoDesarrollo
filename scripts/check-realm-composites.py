#!/usr/bin/env python3
"""Verifica que el realm UMBRAL-UCAB defina los permisos funcionales como
realm roles técnicos composite de los roles base (SP-5a, ADR-0013)."""
import json
import sys

REALM = "infra/keycloak/import/umbral-realm.json"
BASE = {"Administrador", "Operador", "Participante"}
TECNICOS = {"GestionarPartidas", "GestionarEquipos", "ParticiparEnPartidas"}
# El realm solo declara el composite fijo. Los privilegios gobernables
# (GestionarPartidas, GestionarEquipos) los pone el reconciliador de Identity
# desde permisos_rol, asi que no deben aparecer declarados aqui.
COMPOSITES = {
    "Participante": {"ParticiparEnPartidas"},
}
NO_COMPOSITE = {"Administrador", "Operador"}

def fail(msg):
    print(f"FAIL: {msg}")
    sys.exit(1)

with open(REALM) as f:
    realm = json.load(f)

roles = {r["name"]: r for r in realm.get("roles", {}).get("realm", [])}

if set(roles) != BASE | TECNICOS:
    fail(f"roles realm = {sorted(roles)}; esperado {sorted(BASE | TECNICOS)}")

for base_role, expected in COMPOSITES.items():
    r = roles[base_role]
    if not r.get("composite"):
        fail(f"{base_role} no es composite")
    got = set(r.get("composites", {}).get("realm", []))
    if got != expected:
        fail(f"{base_role} composites = {sorted(got)}; esperado {sorted(expected)}")

for base_role in NO_COMPOSITE:
    if roles[base_role].get("composite"):
        fail(f"{base_role} no debe declarar composites: sus privilegios los gobierna permisos_rol")

for t in TECNICOS:
    if roles[t].get("composite"):
        fail(f"{t} debe ser rol simple")

for user in realm.get("users", []):
    directos = TECNICOS & set(user.get("realmRoles", []))
    if directos:
        fail(f"usuario {user.get('username')} tiene roles técnicos directos: {sorted(directos)}")

print("OK: composites de permisos funcionales correctos")
