# Deploy no Coolify com Cloudflare Tunnel

Checklist de produção para a Iscuissati's Saboaria Artesanal.
Ordem: infraestrutura → banco → aplicação → domínio → verificação → rotina.

---

## 1. Pré-requisitos

- [ ] VPS com Docker (o Coolify instala o resto).
- [ ] Domínio com DNS no Cloudflare (proxy ligado para o site).
- [ ] Conta no Coolify acessível por HTTPS.
- [ ] Repositório `iscuissati-saboaria-artesanal` conectado ao Coolify (GitHub App ou deploy key).
- [ ] Cloudflare Tunnel criado (`cloudflared`) apontando para o serviço da aplicação.

---

## 2. PostgreSQL (serviço separado no Coolify)

- [ ] Criar recurso **PostgreSQL 17** no mesmo projeto do Coolify.
- [ ] Anotar: host interno, porta, banco, usuário e **senha forte** (gerar, não reutilizar).
- [ ] Habilitar **backup automático diário** no próprio recurso.
- [ ] Confirmar que o banco **não** está exposto publicamente (sem porta publicada).

> O volume do banco precisa estar em disco persistente. Sem isso, todo deploy que recria o
> container apaga a loja.

---

## 3. Aplicação (Dockerfile)

- [ ] Novo recurso → **Dockerfile** → branch `main`, porta **8080**.
- [ ] Volume persistente para imagens: mount em `/app/media`.
- [ ] Volume persistente para chaves de criptografia: mount em `/app/keys`.
- [ ] Healthcheck HTTP: caminho `/health`, intervalo 30 s, timeout 5 s, retries 5.
- [ ] Estratégia de deploy: **rolling** ou parar-e-subir (a app aplica migrações no start).

### Variáveis de ambiente (obrigatórias)

| Variável | Valor / observação |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `ConnectionStrings__DefaultConnection` | `Host=<host-interno>;Port=5432;Database=iscuissatis;Username=<user>;Password=<senha>` |
| `Database__MigrateOnStartup` | `true` |
| `Seed__AdminEmail` | e-mail real do administrador |
| `Seed__AdminPassword` | senha forte (mínimo 10, com maiúscula) |
| `Security__UseHttpsRedirection` | `false` (TLS termina no Cloudflare) |
| `Security__TrustForwardedHeaders` | `true` (atrás do Tunnel) |
| `Security__DataProtectionPath` | `/app/keys` |
| `Storage__RootPath` | `/app/media` |
| `Storage__PublicPrefix` | `/media` |
| `Payments__Provider` | `fake` até o gateway real ser escolhido |
| `Payments__WebhookSecret` | segredo longo e aleatório |
| `Payments__ExpirationWorker` | `true` |
| `Analytics__Ga4Id` | deixar vazio até existir consentimento e ID |
| `Email__Provider` | `resend` |
| `Email__ResendApiKey` | chave criada no painel do Resend (nunca no repositório) |
| `Email__From` | remetente **verificado** no Resend, ex.: `Iscuissati's Saboaria <no-reply@seudominio.com.br>` |

> **Nunca** ligue `Database__SeedDemo=true` em produção: ele cria um produto fictício publicado.

---

## 4. Domínio e Cloudflare

- [ ] Cloudflare Tunnel: rota `loja.seudominio.com.br` → `http://<container>:8080`.
- [ ] SSL/TLS do Cloudflare em **Full** (o Tunnel já entrega TLS na borda).
- [ ] Confirmar que a app enxerga `X-Forwarded-Proto: https` (senão cookie de login sai como não-secure).
- [ ] Regras de cache: **bypass** para `/admin`, `/checkout`, `/minha-conta`, `/sacola`, `/webhooks`.
- [ ] WAF: regra de rate limit para `/entrar` (proteção extra ao lockout do Identity).

---

## 5. Verificação pós-deploy (fazer nesta ordem)

- [ ] `GET /health` → **200** e `Healthy`.
- [ ] `/` carrega com o catálogo e o rodapé.
- [ ] `/admin` redireciona para `/entrar`.
- [ ] Login do admin com `Seed__AdminEmail` → cai na configuração de 2FA → ativa com o celular → guarda os 10 códigos de recuperação.
- [ ] Criar categoria → criar produto → subir imagem → **publicar** (só publica com imagem + variação ativa).
- [ ] Compra de teste como visitante: sacola → checkout com CEP → pedido "aguardando pagamento".
- [ ] `/admin/pedidos` mostra o pedido; cancelar devolve o estoque.
- [ ] `/sitemap.xml` lista o produto publicado; `/robots.txt` bloqueia `/admin`.
- [ ] **Refazer login depois de um deploy**: se pedir login de novo, o volume de chaves não está montado.
- [ ] Domínio verificado no Resend (registros SPF/DKIM publicados) e **e-mail de teste recebido** de verdade
      — crie um pedido de teste e confirme que a confirmação chegou em uma caixa real, não em spam.

---

## 6. Backup e restauração (testar antes de lançar)

```bash
# backup manual (também agende no Coolify)
./scripts/backup-postgres.sh
```

- [ ] Backup automático diário ativo e **cópia fora do servidor** (S3/R2 ou outro host).
- [ ] **Teste de restore** feito uma vez: banco novo → `pg_restore` → app aponta para ele → catálogo aparece.
- [ ] Backup do volume `/app/media` (as imagens não estão no banco).

---

## 7. Monitoramento e rotina

- [ ] Alerta de indisponibilidade no Cloudflare (health check) ou UptimeRobot.
- [ ] Coolify → notificações de deploy e de falha no Telegram/e-mail.
- [ ] Logs: `docker logs` pelo Coolify; erros 5xx observados na primeira semana.
- [ ] Revisar semanalmente: pedidos presos em `PendingPayment`, estoque baixo, backups íntegros.

---

## 8. Antes de divulgar (bloqueios de negócio)

- [ ] Gateway de pagamento decidido e adaptador implementado (hoje `fake`).
- [ ] Faixas de frete reais em `Shipping:Bands` (as atuais são exemplo).
- [ ] Provedor de e-mail configurado (hoje os e-mails só vão para o log).
- [ ] Logo vetorial, fotos reais e textos definitivos.
- [ ] Razão social, CNPJ e endereço no rodapé (CDC).
- [ ] ANVISA/rotulagem e regime tributário validados com contador.
- [ ] Política de privacidade revisada e encarregado de dados definido.

---

## 9. Rollback

1. Coolify → Deployments → escolher o deploy anterior → **Redeploy**.
2. Se a migração for incompatível: restaurar o backup anterior e apontar a app para ele.
3. Migrações novas **nunca** são editadas; correção entra em migration nova.
