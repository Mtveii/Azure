window.translationTask = 0;

document.addEventListener(
    'selectionchange', () => {
        const fragment = document.getSelection().toString();
        console.log(fragment);
        if (window.translationTask != 0) {
            clearTimeout(window.translationTask);
        }
        window.translationTask = setTimeout(
            () => translate(fragment),
            1000
        );
    }
);

function translate(fragment) {
    fragment = fragment.trim();
    if (fragment.length > 0) {
        const langFrom = document.querySelector('select[name="lang-from"]').value;
        const langTo = document.querySelector('select[name="lang-to"]').value;

        console.log("Translated: ", fragment);
        fetch(`/Home/FetchTranslation?lang-from=${langFrom}&lang-to=${langTo}&original-text=${fragment}&action-button=fetch`)
            .then(r => r.json())
            .then(j => {
                alert(j);
            });
    }    
    window.translationTask = 0;
}
/*
Д.З. Додати до результату перекладу і сам текст, що перекладався:
[development - розробка]
* якщо довжина тексту велика, то розділяти розривом рядка, інакше - символом тире
У разі помилки (як 400, так і 500) виводити повідомлення про 
тимчасову непридатність сервісу перекладу
*/