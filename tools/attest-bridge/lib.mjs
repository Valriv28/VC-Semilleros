// Núcleo compartido del puente con el contrato de Attest.
// Habla con el contrato YA DESPLEGADO (no despliega nada) y firma con la cuenta de la plataforma.
// Mismo patrón que el ejemplo de clase (poll-dapp): cliente del contrato → simular → firmar → enviar → confirmar.
import { Keypair, TransactionBuilder } from "@stellar/stellar-sdk";
import { Client } from "@stellar/stellar-sdk/contract";
import { Api } from "@stellar/stellar-sdk/rpc";

export function need(name) {
  const v = process.env[name];
  if (!v) {
    console.error(`Falta la variable de entorno ${name} (ver .env.example)`);
    process.exit(1);
  }
  return v;
}

export const cfg = {
  rpcUrl: need("STELLAR_RPC_URL"),
  passphrase: need("NETWORK_PASSPHRASE"),
  contractId: need("ATTEST_CONTRACT_ID"),
  explorer: `https://stellar.expert/explorer/${process.env.EXPLORER_NETWORK ?? "testnet"}`,
  keypair: Keypair.fromSecret(need("PLATFORM_SECRET")),
};

// Firmador con la misma forma que devuelve una wallet: { signedTxXdr, signerAddress }.
const signTransaction = async (xdr) => {
  const tx = TransactionBuilder.fromXDR(xdr, cfg.passphrase);
  tx.sign(cfg.keypair);
  return { signedTxXdr: tx.toXDR(), signerAddress: cfg.keypair.publicKey() };
};

let client;
/** Cliente del contrato; lee su interfaz (spec) directamente de la red. */
export async function getClient() {
  client ??= await Client.from({
    contractId: cfg.contractId,
    networkPassphrase: cfg.passphrase,
    rpcUrl: cfg.rpcUrl,
    publicKey: cfg.keypair.publicKey(),
  });
  return client;
}

/**
 * Los métodos del contrato devuelven Result<T, Error>. Si vino un Err, lanza un error que
 * `contractCodeOf` sabe leer; si vino Ok, devuelve T. (Si no es un Result, lo devuelve tal cual.)
 */
export function unwrap(result) {
  if (result && typeof result.isErr === "function") {
    if (result.isErr()) {
      const e = result.unwrapErr?.();
      // El SDK entrega el caso del enum de errores: { message: "AlreadyRevoked" } (a veces con code).
      throw Object.assign(new Error("contract_error"), { contractName: e?.message, contractCode: Number(e?.code) });
    }
    return result.unwrap();
  }
  return result;
}

/** Simulación + firma + envío + espera. "Enviada" no es "confirmada": se revisa el estado final. */
export async function sendTx(tx) {
  if (tx.simulation && Api.isSimulationError(tx.simulation)) throw new Error(tx.simulation.error);
  const sent = await tx.signAndSend({ signTransaction });
  const hash = sent.sendTransactionResponse?.hash ?? null;
  if (sent.getTransactionResponse?.status !== Api.GetTransactionStatus.SUCCESS) {
    throw Object.assign(new Error("La transacción falló en la red"), { hash });
  }
  return { hash, explorerUrl: `${cfg.explorer}/tx/${hash}` };
}

/** Errores del contrato de Attest (contracts/stellar/protocol/src/errors.rs): [código del puente, HTTP]. */
export const CONTRACT_ERRORS = {
  3: ["esquema_no_encontrado", 400],
  5: ["no_encontrada", 404],
  6: ["no_autorizado", 403],
  15: ["no_revocable", 409],
  22: ["expirada", 410],
  23: ["fecha_invalida", 400],
  28: ["esquema_ya_existe", 409],
  29: ["ya_revocada", 409],
};
const CODE_BY_NAME = {
  SchemaNotFound: 3,
  AttestationNotFound: 5,
  NotAuthorized: 6,
  AttestationNotRevocable: 15,
  AttestationExpired: 22,
  InvalidDeadline: 23,
  SchemaAlreadyExists: 28,
  AlreadyRevoked: 29,
};

/** Número del error del contrato, venga como `Error(Contract, #N)`, como Err desenvuelto o por nombre. */
export function contractCodeOf(e) {
  if (Number.isFinite(e?.contractCode)) return e.contractCode;
  const m = /Error\(Contract, #(\d+)\)/.exec(String(e?.message ?? e));
  if (m) return Number(m[1]);
  return CODE_BY_NAME[e?.contractName] ?? null;
}
