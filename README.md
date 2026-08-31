# GSBC.Accounting Helm repository

This branch is a Helm chart repository, published by GitHub Pages at
<https://asherslab.github.io/GSBC.Accounting>.

Nothing here is written by hand. `helm/chart-releaser-action`, run from
`.github/workflows/chart-publish.yml` on `master`, packages `Charts/accounting` and updates
`index.yaml`. The branch has to exist for that action to work, which is the only reason this file
was committed.

Argo CD reads this repository — see `clusters/mini/app-definitions/accounting.yaml` in
`Asherslab/gsbc.argo`.
