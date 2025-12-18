// ui controller
const el = (id) => document.getElementById(id);

const state = { timer: null };

const GATEWAY = "http://localhost:8080";

function now() {
    return new Date().toISOString();
}

function logLine(level, msg, obj) {
    const base = `[${now()}] ${level}: ${msg}`;
    const tail = obj ? `\n${JSON.stringify(obj, null, 2)}\n` : "\n";
    el("log").textContent = base + tail + el("log").textContent;
}

function setBalance(v) {
    el("balanceVal").textContent = v === null || v === undefined ? "—" : String(v);
}

function userId() {
    return el("userId").value.trim();
}

function assertUser() {
    if (!userId()) {
        logLine("предупреждение", "укажите x-user-id или сгенерируйте его");
        return false;
    }
    return true;
}

function genGuid() {
    return crypto.randomUUID();
}

function saveUser() {
    localStorage.setItem("gozon.userId", userId());
}

function loadUser() {
    const u = localStorage.getItem("gozon.userId");
    if (u) el("userId").value = u;
}

async function api(method, path, body) {
    if (!assertUser()) throw new Error("нет user id");

    const url = GATEWAY + path;

    const res = await fetch(url, {
        method,
        headers: {
            "Content-Type": "application/json",
            "X-User-Id": userId()
        },
        body: body ? JSON.stringify(body) : undefined
    });

    const text = await res.text();
    let data;
    try { data = text ? JSON.parse(text) : null; }
    catch { data = text; }

    if (!res.ok) {
        logLine("ошибка", `${method} ${path} -> ${res.status}`, data);
        throw new Error(`http ${res.status}`);
    }

    logLine("ок", `${method} ${path} -> ${res.status}`, data);
    return data;
}

function badge(status) {
    if (status === "NEW") return `<span class="badge b-new">НОВЫЙ</span>`;
    if (status === "FINISHED") return `<span class="badge b-ok">ОПЛАЧЕН</span>`;
    if (status === "CANCELLED") return `<span class="badge b-fail">ОТМЕНЕН</span>`;
    return `<span class="badge">${status}</span>`;
}

function fmtTime(iso) {
    try { return new Date(iso).toLocaleString(); }
    catch { return iso || ""; }
}

function renderOrders(items) {
    const body = el("ordersBody");

    if (!items || items.length === 0) {
        body.innerHTML = `<tr><td colspan="6" class="muted">заказов нет</td></tr>`;
        return;
    }

    body.innerHTML = items.map(o => {
        const shortId = String(o.id).slice(0, 8) + "…";
        return `
      <tr>
        <td>${fmtTime(o.createdAtUtc)}</td>
        <td title="${o.id}">
          <div style="display:flex;gap:8px;align-items:center">
            <span>${shortId}</span>
            <button class="btn btn-ghost" data-copy="${o.id}">копировать</button>
          </div>
        </td>
        <td class="right">${o.amountKopeks}</td>
        <td>${badge(o.status)}</td>
        <td>${o.description ?? ""}</td>
        <td class="right">
          <button class="btn btn-ghost" data-get="${o.id}">обновить статус</button>
        </td>
      </tr>
    `;
    }).join("");

    body.querySelectorAll("[data-copy]").forEach(b => {
        b.addEventListener("click", async () => {
            const v = b.getAttribute("data-copy");
            await navigator.clipboard.writeText(v);
            logLine("ок", "id заказа скопирован", { orderId: v });
        });
    });

    body.querySelectorAll("[data-get]").forEach(b => {
        b.addEventListener("click", async () => {
            const id = b.getAttribute("data-get");
            el("orderIdLookup").value = id;
            await lookupOrder();
        });
    });
}

async function refreshBalance() {
    const data = await api("GET", "/payments/accounts/balance");

    setBalance(data.balanceKopeks);

    if (data.exists === false) {
        logLine("инфо", "счет не создан, баланс 0");
    }
}

async function createAccount() {
    await api("POST", "/payments/accounts/create");
    await refreshBalance();
}

async function topUp() {
    const amount = Number(el("topupAmount").value);
    await api("POST", "/payments/accounts/topup", { amountKopeks: amount });
    await refreshBalance();
}

async function createOrder() {
    const amount = Number(el("orderAmount").value);
    const description = el("orderDesc").value || null;

    const created = await api("POST", "/orders", { amountKopeks: amount, description });
    el("orderIdLookup").value = created.id;

    await loadOrders();
}

async function loadOrders() {
    const items = await api("GET", "/orders");
    renderOrders(items);
}

async function lookupOrder() {
    const id = el("orderIdLookup").value.trim();
    if (!id) return;

    const data = await api("GET", `/orders/${id}`);
    logLine("инфо", "статус заказа", { orderId: id, status: data.status });
    await loadOrders();
}

function stopAutoRefresh() {
    if (state.timer) clearInterval(state.timer);
    state.timer = null;
}

function startAutoRefresh() {
    stopAutoRefresh();
    if (!el("autoRefresh").checked) return;

    const every = Number(el("refreshEvery").value);
    state.timer = setInterval(async () => {
        try { await loadOrders(); } catch {}
    }, every);
}

function wire() {
    el("genUser").addEventListener("click", () => {
        el("userId").value = genGuid();
        saveUser();
        logLine("ок", "user id сгенерирован", { userId: userId() });
    });

    el("saveUser").addEventListener("click", () => {
        saveUser();
        logLine("ок", "user id сохранен", { userId: userId() });
    });

    el("clearUser").addEventListener("click", () => {
        el("userId").value = "";
        saveUser();
        logLine("ок", "user id очищен");
        setBalance("—");
        renderOrders([]);
    });

    el("createAccount").addEventListener("click", createAccount);
    el("refreshBalance").addEventListener("click", refreshBalance);
    el("topupBtn").addEventListener("click", topUp);

    el("createOrder").addEventListener("click", createOrder);
    el("reloadOrders").addEventListener("click", loadOrders);
    el("lookupBtn").addEventListener("click", lookupOrder);

    el("autoRefresh").addEventListener("change", startAutoRefresh);
    el("refreshEvery").addEventListener("change", startAutoRefresh);

    el("clearLog").addEventListener("click", () => el("log").textContent = "");

    window.addEventListener("beforeunload", stopAutoRefresh);
}

async function boot() {
    loadUser();
    wire();

    logLine("инфо", "ui запущен", { gateway: GATEWAY });

    if (userId()) {
        try {
            await refreshBalance();
            await loadOrders();
        } catch {
        }
        startAutoRefresh();
    }
}

boot();