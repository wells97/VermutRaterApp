const functions = require("firebase-functions");
const admin = require("firebase-admin");

if (!admin.apps.length) {
  admin.initializeApp();
}

/**
 * Mantiene /stats/{vermutId} = {count, sum, avg}
 * cuando se crea/edita/borra un voto en /votosPorUsuario/{vermutId}/{userId}
 */
exports.onVoteWrite = functions.database
    .ref("/votosPorUsuario/{vermutId}/{userId}")
    .onWrite(async (change, context) => {
      const vermutId = context.params.vermutId;

      const before = change.before.exists() ? change.before.val() : null;
      const after = change.after.exists() ? change.after.val() : null;

      const statsRef = admin.database().ref(`/stats/${vermutId}`);

      await statsRef.transaction((s) => {
        const state = s || {count: 0, sum: 0, avg: 0};

        // voto nuevo
        if (before === null && after !== null) {
          state.count += 1;
        }

        // voto borrado
        if (before !== null && after === null) {
          state.count -= 1;
        }

        // voto editado -> quito el anterior
        if (before !== null && after !== null) {
          state.sum -= Number(before);
        }

        // añado el nuevo (o el único si es alta)
        if (after !== null) {
          state.sum += Number(after);
        }

        state.avg = state.count > 0 ? state.sum / state.count : 0;
        return state;
      });

      return null;
    });
