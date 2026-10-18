// Se corre UNA vez: registra el esquema de VC-Semilleros en el contrato de Attest.
// Imprime el schema UID para ponerlo en .env como ATTEST_SCHEMA_UID.
import { cfg, getClient, unwrap, sendTx, contractCodeOf } from "./lib.mjs";

// Attest NO valida el valor contra esta definición (queda como documentación del formato).
const DEFINICION = "v:u32,tipo:string,ref:string,sha256:string";
const REVOCABLE = true; // necesario para poder revocar credenciales

try {
  const c = await getClient();
  const tx = await c.register({
    caller: cfg.keypair.publicKey(),
    schema_definition: DEFINICION,
    resolver: undefined,
    revocable: REVOCABLE,
  });
  const uid = Buffer.from(unwrap(tx.result)).toString("hex");
  const { explorerUrl } = await sendTx(tx);
  console.log(`Esquema registrado.\nATTEST_SCHEMA_UID=${uid}\nTransacción: ${explorerUrl}`);
} catch (e) {
  if (contractCodeOf(e) === 28) {
    console.error("Ese esquema ya estaba registrado con esta cuenta (error #28). Reutiliza su UID.");
  } else {
    console.error(e);
  }
  process.exit(1);
}
