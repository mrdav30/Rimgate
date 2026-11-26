-- Read-only optional-mod contracts on an already loaded owned private save.
-- Pass configPath, modRoot, expectedActivePackagesCsv and optional patientId.
-- Use an empty CSV for the absent profile; explicitly enumerate the intended
-- package IDs for a present profile. This never changes mods, subjects or jobs.
-- Compile through the installed SDK; host must pause in finally on failure.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
local report = rb.call("rimgate/optional_integration_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, expectedActivePackagesCsv = params.expectedActivePackagesCsv, patientId = params.patientId })
rb.assert(report.success and report.result.success and report.result.expectedPackagesMatch, "Optional package set, resolved XML or registered hooks did not match the expected profile; retain the report before diagnosing gameplay.")
return { success = true, report = report.result, limits = "Content and registration checks only; use real admission, diagnosis, treatment and facility checks for gameplay acceptance." }
