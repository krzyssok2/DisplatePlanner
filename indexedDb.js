window.indexedDbHelper = {
    db: null,

    openDb: function (dbName, version) {
        return new Promise((resolve, reject) => {
            let request = indexedDB.open(dbName, version);
            request.onupgradeneeded = function (event) {
                let db = event.target.result;
                if (!db.objectStoreNames.contains("userAddedPlates")) {
                    db.createObjectStore("userAddedPlates", { keyPath: "id" }); // Use Id as the primary key
                }
            };
            request.onsuccess = function (event) {
                window.indexedDbHelper.db = event.target.result;
                resolve();
            };
            request.onerror = function (event) {
                reject(event.target.error);
            };
        });
    },

    savePlate: function (plateData) {
        return new Promise((resolve, reject) => {
            let transaction = window.indexedDbHelper.db.transaction(["userAddedPlates"], "readwrite");
            let store = transaction.objectStore("userAddedPlates");
            let request = store.put(plateData); // If id exists, it updates; otherwise, it inserts

            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    },

    getPlate: function (id) {
        return new Promise((resolve, reject) => {
            let transaction = window.indexedDbHelper.db.transaction(["userAddedPlates"], "readonly");
            let store = transaction.objectStore("userAddedPlates");
            let request = store.get(id);

            request.onsuccess = () => resolve(request.result || null);
            request.onerror = () => reject(request.error);
        });
    },

    getAllPlates: function () {
        return new Promise((resolve, reject) => {
            let transaction = window.indexedDbHelper.db.transaction(["userAddedPlates"], "readonly");
            let store = transaction.objectStore("userAddedPlates");
            let request = store.getAll();

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    deletePlate: function (id) {
        return new Promise((resolve, reject) => {
            let transaction = window.indexedDbHelper.db.transaction(["userAddedPlates"], "readwrite");
            let store = transaction.objectStore("userAddedPlates");
            let request = store.delete(id);

            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    },

    clearPlates: function () {
        return new Promise((resolve, reject) => {
            let transaction = window.indexedDbHelper.db.transaction(["userAddedPlates"], "readwrite");
            let store = transaction.objectStore("userAddedPlates");
            let request = store.clear();

            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    }
};