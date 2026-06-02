import test from "node:test";
import assert from "node:assert/strict";
import {
  getLeaveTeamNoActiveTeamDescription,
  getLeaveTeamSuccessMessage,
  leaveTeamNoActiveTeamTitle,
} from "../src/features/teams/leaveTeamScreenContent.js";

test("leave team screen success content should explicitly show no active team state", () => {
  assert.equal(leaveTeamNoActiveTeamTitle, "Sin equipo activo");
  assert.match(getLeaveTeamSuccessMessage(), /Ya no perteneces a ningun equipo activo/);
  assert.match(getLeaveTeamNoActiveTeamDescription(), /crear un equipo nuevo|unirte a otro/);
});
