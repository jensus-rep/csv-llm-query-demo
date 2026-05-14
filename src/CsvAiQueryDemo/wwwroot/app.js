const examples = [
    "Wie oft kommt der Vorname Max vor?",
    "Wie viele unterschiedliche Vornamen gibt es?",
    "Welche Vornamen kommen am häufigsten vor?",
    "Zeige mir alle Einträge, bei denen der Nachname Müller ist.",
    "Wie viele Mail Adressen enthalten example.com?"
];

const pipelineDefaults = [
    "CSV lokal geladen",
    "Dataset Profile erzeugt",
    "Nutzerfrage empfangen",
    "QueryIntent durch LLM erzeugt",
    "QueryEngine in C# ausgeführt",
    "Ergebnis zurückgegeben"
].map(name => ({ name, status: "pending", description: "" }));

const messages = document.querySelector("#messages");
const form = document.querySelector("#chat-form");
const input = document.querySelector("#message-input");
const examplesContainer = document.querySelector("#examples");

initialize();

async function initialize() {
    renderPipeline(pipelineDefaults);
    renderTokenUsage();
    renderExamples();
    await loadDatasetProfile();
}

async function loadDatasetProfile() {
    const status = document.querySelector("#dataset-status");
    try {
        const response = await fetch("/api/dataset/profile");
        const profile = await response.json();
        status.textContent = "ready";
        renderDataset(profile);
        renderDatasetProfileJson(profile);
    } catch {
        status.textContent = "error";
    }
}

// Render all dynamic text through escapeHtml because the values originate from
// CSV data, model output, or API responses.
function renderDataset(profile) {
    document.querySelector("#dataset-summary").innerHTML = `
        <div class="metric"><span>Datei</span><strong>${escapeHtml(profile.fileName)}</strong></div>
        <div class="metric"><span>Zeilen</span><strong>${profile.rowCount}</strong></div>
        <div class="metric"><span>Spalten</span><strong>${profile.columnCount}</strong></div>
    `;

    document.querySelector("#columns").innerHTML = profile.columns.map(column => {
        const topValues = Object.entries(column.topValues ?? {})
            .map(([value, count]) => `${escapeHtml(value)} (${count})`)
            .join(", ");

        return `
            <article class="column">
                <div class="column-header">
                    <span class="column-name">${escapeHtml(column.name)}</span>
                    <span class="tag">${escapeHtml(column.inferredType)}</span>
                </div>
                <p class="top-values">${topValues || "Keine Top Values"}</p>
            </article>
        `;
    }).join("");
}

function renderExamples() {
    examplesContainer.innerHTML = "";
    for (const question of examples) {
        const button = document.createElement("button");
        button.type = "button";
        button.textContent = question;
        button.addEventListener("click", () => {
            input.value = question;
            form.requestSubmit();
        });
        examplesContainer.appendChild(button);
    }
}

form.addEventListener("submit", async event => {
    event.preventDefault();
    const message = input.value.trim();
    if (!message) {
        return;
    }

    addMessage("user", message);
    input.value = "";
    renderPipeline(pipelineDefaults.map(step => ({ ...step })));

    const pending = addMessage("assistant", "Die Anfrage wird verarbeitet ...");
    try {
        const response = await fetch("/api/chat", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ message })
        });
        const payload = await response.json();

        pending.textContent = payload.answer;
        renderPipeline(payload.pipelineSteps ?? pipelineDefaults);
        renderDatasetProfileJson(payload.datasetProfile);
        renderPrompt(payload.queryIntentPrompt);
        renderTokenUsage(payload.tokenUsage);
        document.querySelector("#query-intent-json").textContent = JSON.stringify(payload.queryIntent ?? {}, null, 2);
        document.querySelector("#query-result-json").textContent = JSON.stringify(payload.queryResult ?? {}, null, 2);
    } catch {
        pending.textContent = "Die Anfrage konnte nicht verarbeitet werden.";
    }
});

function addMessage(role, text) {
    const element = document.createElement("div");
    element.className = `message ${role}`;
    element.textContent = text;
    messages.appendChild(element);
    messages.scrollTop = messages.scrollHeight;
    return element;
}

function renderDatasetProfileJson(profile) {
    document.querySelector("#dataset-profile-json").textContent = JSON.stringify(profile ?? {}, null, 2);
}

function renderPrompt(prompt) {
    const provider = getProperty(prompt, "provider");
    const systemPrompt = getProperty(prompt, "systemPrompt");
    const userPrompt = getProperty(prompt, "userPrompt");
    const requestPayload = getProperty(prompt, "requestPayload");

    document.querySelector("#query-prompt-provider").textContent = provider
        ? `Provider: ${provider}`
        : "Noch keine Anfrage gesendet.";
    document.querySelector("#query-system-prompt").textContent = systemPrompt ?? "Noch keine Anfrage gesendet.";
    document.querySelector("#query-user-prompt").textContent = userPrompt ?? "Noch keine Anfrage gesendet.";
    document.querySelector("#query-request-payload").textContent = formatJsonText(requestPayload);
}

function renderTokenUsage(usage = {}) {
    const inputTokens = getProperty(usage, "inputTokens") ?? 0;
    const outputTokens = getProperty(usage, "outputTokens") ?? 0;
    const totalTokens = getProperty(usage, "totalTokens") ?? inputTokens + outputTokens;

    document.querySelector("#token-input").textContent = formatNumber(inputTokens);
    document.querySelector("#token-output").textContent = formatNumber(outputTokens);
    document.querySelector("#token-total").textContent = formatNumber(totalTokens);
}

function formatNumber(value) {
    return new Intl.NumberFormat("de-DE").format(Number(value) || 0);
}

function formatJsonText(value) {
    if (!value) {
        return "Noch keine Anfrage gesendet.";
    }

    if (typeof value !== "string") {
        return JSON.stringify(value, null, 2);
    }

    try {
        return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
        return value;
    }
}

function getProperty(value, camelCaseName) {
    if (!value) {
        return undefined;
    }

    const pascalCaseName = camelCaseName.charAt(0).toUpperCase() + camelCaseName.slice(1);
    return value[camelCaseName] ?? value[pascalCaseName];
}

function renderPipeline(steps) {
    const pipeline = document.querySelector("#pipeline");
    pipeline.innerHTML = steps.map(step => `
        <li class="${escapeHtml(step.status)}">
            <div class="pipeline-name">
                <span class="status-dot"></span>
                <span>${escapeHtml(step.name)}</span>
            </div>
            <div class="pipeline-description">${escapeHtml(step.description || step.status)}</div>
        </li>
    `).join("");
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
