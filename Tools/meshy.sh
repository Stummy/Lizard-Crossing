#!/usr/bin/env bash
# Meshy AI helper for Lizard Crossing — text/image -> 3D asset generation.
#
# The API key is read from OUTSIDE the repo (default ~/.lizard_secrets/meshy_api_key)
# so it is NEVER committed. Override with MESHY_KEY_FILE=/path env var.
# Dependency-free (curl only). NOTE: prompts must not contain double-quote (") chars.
#
# Pipeline: t23 (preview) -> wait -> refine (textures) -> wait -> download .glb,
# then import + normalize + LOD + vet tris in Unity (see docs/MESHY_PIPELINE.md).
set -euo pipefail
KEY_FILE="${MESHY_KEY_FILE:-$HOME/.lizard_secrets/meshy_api_key}"
[ -f "$KEY_FILE" ] || { echo "No Meshy key at $KEY_FILE (store it there, outside the repo)" >&2; exit 1; }
KEY="$(cat "$KEY_FILE")"
BASE="https://api.meshy.ai/openapi"
AUTH=(-H "Authorization: Bearer $KEY")
JSON=(-H "Content-Type: application/json")

usage() { cat <<'EOF'
Usage:
  meshy.sh balance                                  # remaining credits
  meshy.sh t23 "<prompt>" [art_style] [polycount]   # text->3D PREVIEW (no " in prompt)
                                                    #   art_style=realistic|sculpture (def realistic)
                                                    #   polycount default 20000 (keep mobile-sane)
  meshy.sh refine <preview_task_id>                 # refine PREVIEW -> textured model
  meshy.sh status <task_id>                         # raw JSON (text-to-3d task)
  meshy.sh wait <task_id>                           # poll until SUCCEEDED/FAILED
  meshy.sh glb <task_id>                            # print finished GLB url
  meshy.sh download <task_id> <out.glb>             # download finished GLB
EOF
}

field() { sed -n "s/.*\"$1\":\"\([^\"]*\)\".*/\1/p" | head -1; }   # string field
numf()  { sed -n "s/.*\"$1\":\([0-9][0-9]*\).*/\1/p" | head -1; }  # numeric field

cmd="${1:-}"; shift || true
case "$cmd" in
  balance) curl -s "${AUTH[@]}" "$BASE/v1/balance"; echo ;;
  t23)
    prompt="${1:?need prompt}"; style="${2:-realistic}"; poly="${3:-20000}"
    body="{\"mode\":\"preview\",\"prompt\":\"$prompt\",\"art_style\":\"$style\",\"should_remesh\":true,\"target_polycount\":$poly}"
    curl -s "${AUTH[@]}" "${JSON[@]}" -d "$body" "$BASE/v2/text-to-3d"; echo ;;
  refine)
    id="${1:?need preview task id}"
    curl -s "${AUTH[@]}" "${JSON[@]}" -d "{\"mode\":\"refine\",\"preview_task_id\":\"$id\"}" "$BASE/v2/text-to-3d"; echo ;;
  status) id="${1:?need id}"; curl -s "${AUTH[@]}" "$BASE/v2/text-to-3d/$id" ;;
  wait)
    id="${1:?need id}"
    for i in $(seq 1 180); do
      r="$(curl -s "${AUTH[@]}" "$BASE/v2/text-to-3d/$id")"
      st="$(printf '%s' "$r" | field status)"; pr="$(printf '%s' "$r" | numf progress)"
      echo "[$i] status=${st:-?} progress=${pr:-?}"
      case "$st" in SUCCEEDED|FAILED|CANCELED) exit 0 ;; esac
      sleep 5
    done ;;
  glb) id="${1:?need id}"; curl -s "${AUTH[@]}" "$BASE/v2/text-to-3d/$id" | field glb ;;
  download)
    id="${1:?need id}"; out="${2:?need out path}"
    url="$(curl -s "${AUTH[@]}" "$BASE/v2/text-to-3d/$id" | field glb)"
    [ -n "$url" ] || { echo "no glb url yet (task not finished?)" >&2; exit 1; }
    mkdir -p "$(dirname "$out")"; curl -s -L "$url" -o "$out"
    echo "saved $out ($(wc -c < "$out") bytes)" ;;
  *) usage ;;
esac
