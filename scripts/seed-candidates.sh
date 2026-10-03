#!/usr/bin/env bash
# Popula o banco com candidatos válidos pela API, para ver a listagem, a paginação e os detalhes.
# Tudo é fictício: nomes de personagens de Game of Thrones e Harry Potter, e-mails em example.com
# e telefones no padrão 9 0000.
# O fim do nome tem a inicial de cada campo preenchido: N nome, E e-mail, T telefone,
# C cargo, R resumo. Ex.: NER = nome, e-mail e resumo; NETCR = todos os campos.
#
# Uso: scripts/seed-candidates.sh [URL_DA_API] [QUANTIDADE_PARA_PAGINACAO]
#   API local (dotnet run): http://localhost:5206 (padrão)
#   API no Docker:          http://localhost:8080
#
# Cada execução usa e-mails novos (prefixo com a hora), então pode rodar quantas vezes quiser.

BASE_URL="${1:-http://localhost:5206}"
BULK_COUNT="${2:-30}"
RUN_ID="$(date +%s)"

created=0
failed=0
counter=0

# Escapa o texto para ir dentro de uma string JSON.
esc() {
  local s="$1"
  s="${s//\\/\\\\}"
  s="${s//\"/\\\"}"
  s="${s//$'\n'/\\n}"
  printf '%s' "$s"
}

# Repete o texto até ter exatamente N caracteres (usar só ASCII para a conta bater).
fill() {
  local unit="$1" size="$2" text="$1"
  while ((${#text} < size)); do text+="$unit"; done
  text="${text:0:size}"
  # A API remove espaços das pontas: terminar em espaço faria o texto ter menos que o tamanho pedido.
  if [[ "${text: -1}" == " " ]]; then text="${text:0:size-1}."; fi
  printf '%s' "$text"
}

# Telefones fictícios: celular 41 9 000x xxxx (11 dígitos) e fixo 41 3000 xxxx (10 dígitos).
mobile() { printf '%s9000%05d' "${2:-41}" "$1"; }
landline() { printf '413000%04d' "$1"; }

# add EMAIL NOME TELEFONE CARGO RESUMO [DESCRIÇÃO]: o que vier vazio não é enviado.
# O nome sai com as iniciais dos campos preenchidos no fim.
add() {
  local email="$1" base="$2" phone="${3:-}" position="${4:-}" summary="${5:-}" desc="${6:-}"
  initials="NE"
  if [[ -n "$phone" ]]; then initials+="T"; fi
  if [[ -n "$position" ]]; then initials+="C"; fi
  if [[ -n "$summary" ]]; then initials+="R"; fi
  local name="$base $initials"

  local body="{\"name\":\"$(esc "$name")\",\"email\":\"$(esc "$email")\""
  if [[ -n "$phone" ]]; then body+=",\"phone\":\"$(esc "$phone")\""; fi
  if [[ -n "$position" ]]; then body+=",\"position\":\"$(esc "$position")\""; fi
  if [[ -n "$summary" ]]; then body+=",\"summary\":\"$(esc "$summary")\""; fi
  body+="}"
  post "$name" "$body" "$desc"
}

# send NOME TELEFONE CARGO RESUMO [DESCRIÇÃO]: usa um e-mail novo (seed.<hora>.<número>@example.com).
send() {
  local email
  counter=$((counter + 1))
  printf -v email 'seed.%s.%03d@example.com' "$RUN_ID" "$counter"
  add "$email" "$@"
}

post() {
  local name="$1" body="$2" desc="${3:-}" response code id
  response="$(curl -s -w $'\n%{http_code}' -X POST "$BASE_URL/api/candidates" \
    -H 'Content-Type: application/json; charset=utf-8' --data-binary "$body")"
  code="${response##*$'\n'}"
  if [[ "$code" == "201" ]]; then
    id="$(grep -o '"id":[0-9]*' <<<"$response" | head -1 | cut -d: -f2)"
    printf 'id %-4s %-6s %s%s\n' "$id" "$initials" "${name:0:48}" "${desc:+  ($desc)}"
    created=$((created + 1))
  else
    printf 'HTTP %s  %s\n' "$code" "${name:0:60}"
    failed=$((failed + 1))
  fi
}

if ! curl -s -o /dev/null --max-time 5 "$BASE_URL/api/candidates?pageSize=1"; then
  echo "A API não respondeu em $BASE_URL. Suba a API (ou passe a URL como 1º argumento)." >&2
  exit 1
fi

SHORT_SUMMARY="Desenvolvedora com 3 anos de experiência em React e .NET."
MEDIUM_SUMMARY="Profissional de tecnologia com 6 anos de experiência em desenvolvimento web. Atuou em projetos de e-commerce e serviços financeiros, com APIs em .NET e interfaces em React. Participou de migrações de banco de dados para SQL Server, escreveu testes automatizados e acompanhou entregas em times ágeis. Busca uma posição em que possa unir back-end, front-end e boas práticas de qualidade, com foco em código simples e fácil de manter. Tem facilidade para aprender tecnologias novas, gosta de revisar código em dupla e de documentar as decisões do projeto para o time."
MULTILINE_SUMMARY=$'Experiência\n- 2023-2026: Pessoa desenvolvedora full-stack (.NET e React)\n- 2021-2023: Estágio em suporte e QA\n\nFormação\n- Engenharia de Computação\n\nIdiomas\n- Português (nativo)\n- Inglês (fluente)'

echo "API: $BASE_URL"
echo
echo "== Combinações de campos =="
send "Petyr Baelish"
send "Illyrio Mopatis" "$(mobile 101)"
send "Daario Naharis" "" "Analista de Sistemas"
send "Barristan Selmy" "" "" "$SHORT_SUMMARY"
send "Oberyn Martell" "$(mobile 102 11)" "Desenvolvedora Front-end"
send "Ellaria Sand" "$(mobile 103 21)" "" "$SHORT_SUMMARY"
send "Tormund Giantsbane" "" "Engenheira de Dados" "$SHORT_SUMMARY"
send "Davos Seaworth" "$(mobile 104 47)" "Tech Lead" "$SHORT_SUMMARY"

echo
echo "== Telefones =="
send "Filius Flitwick" "$(landline 105)" "" "" "telefone fixo, 10 dígitos"
send "Pomona Sprout" "$(mobile 106)" "" "" "celular, 11 dígitos"

echo
echo "== Tamanhos =="
# 147 caracteres + " NE" = 150, o limite do nome.
send "$(fill 'Horace Slughorn ' 147)" "" "" "" "nome de 150 caracteres"
# 64 + 63 + 63 + 57 caracteres e os separadores = 254, o limite do e-mail.
long_local="$(fill "seed${RUN_ID}x" 64)"
label_63="$(fill 'd' 63)"
label_57="$(fill 'd' 57)"
add "${long_local}@${label_63}.${label_63}.${label_57}.com" "Sybill Trelawney" "" "" "" "e-mail de 254 caracteres"
send "Gilderoy Lockhart" "" "$(fill 'Cargo no limite de 100 caracteres - ' 100)" "" "cargo de 100 caracteres"
send "Xenophilius Lovegood" "" "" "$SHORT_SUMMARY" "resumo de uma linha"
send "Bellatrix Lestrange" "" "" "$MEDIUM_SUMMARY" "resumo de um parágrafo"
send "Gellert Grindelwald" "" "" "$MULTILINE_SUMMARY" "resumo com quebras de linha"
send "Aberforth Dumbledore" "" "" "$(fill 'Resumo no limite de 2000 caracteres. ' 2000)" "resumo de 2000 caracteres"
# 144 caracteres + " NETCR" = 150.
send "$(fill 'Kingsley Shacklebolt ' 144)" "$(mobile 107)" "$(fill 'Cargo no limite de 100 caracteres - ' 100)" "$(fill 'Resumo no limite de 2000 caracteres. ' 2000)" "todos os campos no tamanho máximo"

echo
echo "== Textos diferentes =="
send "Jaqen H'ghar" "$(mobile 108)" "Analista de Sistemas Pleno" "Experiência com ação social, coordenação de equipes e atendimento ao público." "apóstrofo no nome, acentos no cargo e no resumo"
send "Séamus Finnigan" "" "" "$SHORT_SUMMARY" "acento no nome"
send "Luna Lovegood" "" "" "Gosto de café ☕ e de código limpo 🚀" "emoji"
send "Fred & George <Weasley>" "$(mobile 109)" "Dev \"Full-stack\" & QA <júnior>" "Usa <b>negrito</b> e \"aspas\" no texto; o app deve mostrar tudo como texto." "símbolos &, <, > e aspas"
add "Dolores.Umbridge+${RUN_ID}@Example.COM" "Dolores Umbridge" "" "" "" "e-mail com maiúsculas e +"
add "rita.skeeter.${RUN_ID}@rh.empresa.example.com.br" "Rita Skeeter" "" "" "" "e-mail com subdomínio"
counter=$((counter + 1))
printf -v email 'seed.%s.%03d@example.com' "$RUN_ID" "$counter"
initials="NE"
post "Cornelius Fudge NE" "{\"name\":\"Cornelius Fudge NE\",\"email\":\"$email\",\"phone\":\"\",\"position\":\"\",\"summary\":\"\"}" "telefone, cargo e resumo enviados vazios"

echo
echo "== Paginação ($BULK_COUNT candidatos) =="
NAMES=("Jorah Mormont" "Minerva McGonagall" "Podrick Payne" "Remus Lupin" "Brienne Tarth" "Alastor Moody" "Tywin Lannister" "Nymphadora Tonks" "Walder Frey" "Rubeus Hagrid"
  "Roose Bolton" "Newt Scamander" "Margaery Tyrell" "Garrick Ollivander" "Olenna Tyrell" "Fenrir Greyback" "Loras Tyrell" "Argus Filch" "Selyse Florent" "Alecto Carrow"
  "Stannis Baratheon" "Amycus Carrow" "Yara Greyjoy" "Ludo Bagman" "Euron Greyjoy" "Bathilda Bagshot" "Doran Martell" "Hepzibah Smith" "Arianne Martell" "Cuthbert Binns"
  "Lysa Arryn" "Armando Dippet" "Meera Reed" "Viktor Krum" "Beric Dondarrion" "Fleur Delacour" "Sandor Clegane" "Cedric Diggory" "Samwell Tarly" "Neville Longbottom")
POSITIONS=("Desenvolvedor Back-end" "Analista de QA" "Designer de Produto" "Gerente de Projetos" "Analista de Dados")
for ((i = 1; i <= BULK_COUNT; i++)); do
  # Os 3 bits de i % 8 escolhem telefone, cargo e resumo: percorre as 8 combinações.
  phone="" position="" summary=""
  if ((i % 8 & 1)); then phone="$(mobile $((200 + i)))"; fi
  if ((i % 8 & 2)); then position="${POSITIONS[i % ${#POSITIONS[@]}]}"; fi
  if ((i % 8 & 4)); then summary="$SHORT_SUMMARY"; fi
  send "${NAMES[(i - 1) % ${#NAMES[@]}]}" "$phone" "$position" "$summary"
done

echo
echo "Criados: $created | Fora do esperado: $failed"
echo "Mais recente primeiro: $BASE_URL/api/candidates?page=1&pageSize=10"
((failed == 0))
