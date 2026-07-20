# Label definitions

- `LabelNextQuarterEntry`: for a state not yet entered, true if it first satisfies the configured policy in the immediately following observed quarter, false if that quarter is observed without entry, and null if unavailable, suppressed, already entered, or censored.
- `LabelQuartersUntilEntry`: integer quarter distance under the configured entry policy; null for right-censored non-entry. No sentinel value is used.
- Q4 labels: Q4 means four quarters after the approval quarter (`QuarterSinceApproval = 4`). ND, WD, and Access Gap are attached only when that complete follow-up exists; otherwise all remain null.
- Persistent inequality: configurable Access Gap threshold, consecutive-quarter count, and horizon. The development default is illustrative and is not scientifically selected.
