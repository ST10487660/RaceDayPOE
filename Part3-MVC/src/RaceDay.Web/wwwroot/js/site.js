// Registration page: only ask for the invite code when "Organiser" is chosen.
(function () {
    var block = document.getElementById('inviteBlock');
    if (!block) return;

    function update() {
        var selected = document.querySelector('input[name="Role"]:checked');
        block.hidden = !(selected && selected.value === 'Organiser');
    }

    document.querySelectorAll('input[name="Role"]').forEach(function (radio) {
        radio.addEventListener('change', update);
    });
    update();
})();
