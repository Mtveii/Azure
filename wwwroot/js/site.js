window.translationTask = 0;

document.addEventListener('selectionchange', () => {
    const fragment = document.getSelection().toString();

    if (window.translationTask !== 0) {
        clearTimeout(window.translationTask);
    }

    window.translationTask = setTimeout(() => {
        if (document.getElementById("checkbox").checked) { 
        translate(fragment);
    }
    }, 1000);
});

function translate(fragment) {
    fragment = fragment.trim();

    if (fragment.length === 0) {
        window.translationTask = 0;
        return;
    }

    const langFromEl = document.querySelector('select[name="lang-from"]');
    const langToEl = document.querySelector('select[name="lang-to"]');

    const langFrom = langFromEl ? langFromEl.value : 'uk';
    const langTo = langToEl ? langToEl.value : 'ru';

    fetch(
        `/Home/FetchTranslation?lang-from=${encodeURIComponent(langFrom)}&lang-to=${encodeURIComponent(langTo)}&original-text=${encodeURIComponent(fragment)}&action-button=fetch`
    )
        .then(response => {
            if (!response.ok) {
                throw new Error('Translation service unavailable');
            }
            return response.json();
        })
        .then(translation => {
            const separator = fragment.length > 30 ? '\n' : ' - ';
            alert(`[${fragment}${separator}${translation}]`);
        })
        .catch(() => {
            alert('Сервис перевода временно недоступен');
        })
        .finally(() => {
            window.translationTask = 0;
        });
}
