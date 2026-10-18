// Puente HTTP entre el adaptador C# y el contrato de Attest.
//   POST /anclajes                  ancla el hash de una evidencia (attest)
//   GET  /anclajes/:uid             consulta una evidencia (get_attestation)
//   POST /anclajes/:uid/revocar     revoca (revoke)
// Debe escuchar solo en 127.0.0.1 (o red privada) y exigir X-Bridge-Key: la clave de la plataforma vive aquí.
import { timingSafeEqual } from "node:crypto";
import express from "express";
import { cfg, getClient, unwrap, sendTx, CONTRACT_ERRORS, contractCodeOf, need } from "./lib.mjs";

const SCHEMA_UID = need("ATTEST_SCHEMA_UID"); // 64 hex; se obtiene con `npm run register-schema`
const API_KEY = need("BRIDGE_API_KEY");
const PORT = Number(process.env.PORT ?? 4010);
const HOST = process.env.HOST ?? "127.0.0.1";

class Bad extends Error {
  constructor(codigo, mensaje) {
    super(mensaje);
    this.codigo = codigo;
  }
}

const hexBuf = (hex, bytes, campo) => {
  if (typeof hex !== "string" || !new RegExp(`^[0-9a-fA-F]{${bytes * 2}}$`).test(hex)) {
    throw new Bad("hex_invalido", `${campo} debe ser hexadecimal de ${bytes * 2} caracteres`);
  }
  return Buffer.from(hex, "hex");
};
const toHex = (b) => Buffer.from(b).toString("hex");

// Las escrituras de una misma cuenta se hacen UNA A UNA: evita choques de número de secuencia
// y garantiza que el UID calculado en la simulación sea el que quedará en la cadena (depende del nonce).
let cola = Promise.resolve();
const enSerie = (fn) => {
  const p = cola.then(fn, fn);
  cola = p.catch(() => {});
  return p;
};

const app = express();
app.use(express.json({ limit: "16kb" }));

app.use((req, res, next) => {
  const dado = Buffer.from(String(req.get("x-bridge-key") ?? ""));
  const esperado = Buffer.from(API_KEY);
  if (dado.length !== esperado.length || !timingSafeEqual(dado, esperado)) {
    return res.status(401).json({ codigo: "no_autorizado", mensaje: "Falta o es incorrecta la X-Bridge-Key" });
  }
  next();
});

const handle = (fn) => async (req, res) => {
  try {
    res.json(await fn(req));
  } catch (e) {
    const contractCode = contractCodeOf(e);
    if (e instanceof Bad) return res.status(400).json({ codigo: e.codigo, mensaje: e.message });
    if (contractCode !== null && CONTRACT_ERRORS[contractCode]) {
      const [codigo, status] = CONTRACT_ERRORS[contractCode];
      return res.status(status).json({ codigo, mensaje: `Attest rechazó la operación (#${contractCode})` });
    }
    if (/timeout|timed out|fetch failed|ECONN|ENOTFOUND|EAI_AGAIN|50[234]|Failed to fetch/i.test(String(e?.message ?? e))) {
      return res.status(503).json({ codigo: "red_no_disponible", mensaje: "La red Stellar no respondió" });
    }
    console.error(e);
    res.status(500).json({ codigo: "error_inesperado", mensaje: String(e?.message ?? e), txHash: e?.hash ?? null });
  }
};

app.get("/salud", (_req, res) => res.json({ ok: true, emisora: cfg.keypair.publicKey(), schemaUid: SCHEMA_UID }));

// Lo que se ancla es MÍNIMO: referencia interna + hash. Nada de datos personales.
app.post(
  "/anclajes",
  handle((req) =>
    enSerie(async () => {
      const { referencia, sha256, tipo, expiraEn } = req.body ?? {};
      hexBuf(sha256, 32, "sha256");
      if (typeof referencia !== "string" || !referencia || referencia.length > 64) {
        throw new Bad("referencia_invalida", "referencia es obligatoria (máx. 64 caracteres)");
      }
      if (typeof tipo !== "string" || !tipo || tipo.length > 32) {
        throw new Bad("tipo_invalido", "tipo es obligatorio (máx. 32 caracteres)");
      }
      if (expiraEn != null && !Number.isInteger(expiraEn)) {
        throw new Bad("expira_invalido", "expiraEn debe ser un entero (segundos Unix) o null");
      }

      const value = JSON.stringify({ v: 1, tipo, ref: referencia, sha256: sha256.toLowerCase() });
      const c = await getClient();
      const tx = await c.attest({
        attester: cfg.keypair.publicKey(),
        schema_uid: hexBuf(SCHEMA_UID, 32, "schema_uid"),
        value,
        expiration_time: expiraEn != null ? BigInt(expiraEn) : undefined,
      });
      const uid = unwrap(tx.result); // UID que quedará en la cadena (simulación; las escrituras van en serie)
      const { hash, explorerUrl } = await sendTx(tx);
      return { attestationUid: toHex(uid), txHash: hash, explorerUrl };
    }),
  ),
);

app.get(
  "/anclajes/:uid",
  handle(async (req) => {
    const c = await getClient();
    let a;
    try {
      const tx = await c.get_attestation({ attestation_uid: hexBuf(req.params.uid, 32, "uid") });
      a = unwrap(tx.result);
    } catch (e) {
      if (contractCodeOf(e) === 22) return { estado: "expirada", uid: req.params.uid.toLowerCase() };
      throw e;
    }
    let datos = {};
    try {
      datos = JSON.parse(a.value);
    } catch {
      /* value no era JSON: se devuelve sin referencia ni hash */
    }
    const num = (x) => (x == null ? null : Number(x));
    return {
      estado: a.revoked ? "revocada" : "vigente",
      uid: toHex(a.uid),
      schemaUid: toHex(a.schema_uid),
      attester: a.attester,
      referencia: datos.ref ?? null,
      sha256: datos.sha256 ?? null,
      registradaEn: num(a.timestamp),
      expiraEn: num(a.expiration_time),
      revocadaEn: num(a.revocation_time),
    };
  }),
);

app.post(
  "/anclajes/:uid/revocar",
  handle((req) =>
    enSerie(async () => {
      const c = await getClient();
      const tx = await c.revoke({
        revoker: cfg.keypair.publicKey(), // Attest solo deja revocar a quien atestó
        attestation_uid: hexBuf(req.params.uid, 32, "uid"),
      });
      unwrap(tx.result); // detecta antes de firmar: no autorizada, ya revocada, no revocable…
      const { hash, explorerUrl } = await sendTx(tx);
      return { txHash: hash, explorerUrl };
    }),
  ),
);

app.listen(PORT, HOST, () => console.log(`Puente Attest en http://${HOST}:${PORT} · emisora ${cfg.keypair.publicKey()}`));
