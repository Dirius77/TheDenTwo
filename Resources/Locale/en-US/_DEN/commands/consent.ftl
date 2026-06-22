cmd-setconsent-desc = Sets a consent ID to a value for yourself.
cmd-setconsent-help = {$command} consentId newValue

cmd-setconsent-error-args = Not enough arguments. Usage: {$usage}
cmd-setconsent-error-invalid-consent = {$consentId} is not a ConsentTogglePrototype. Usage: {$usage}
cmd-setconsent-error-bool = {$value} is not a valid boolean. Usage: {$usage}

cmd-setconsent-success = {$consentId} has successfully been set to {$value}.

cmd-consent-desc = Gets any consents that are different to the default value on a player.
cmd-consent-help = {$command}

cmd-consent-no-different = No different consents; all consents are using their defaults.
cmd-consent-differences = Different consents:\n- {$differentConsents}