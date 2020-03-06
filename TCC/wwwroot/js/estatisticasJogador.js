function corrigirDivisaoUsuario(divisaoJogador, nomeJogador) {
    if (window.innerWidth < 576) {
        if (divisaoJogador.innerHTML.length > 0) {
            nomeJogador.style.marginTop = '-90px';
        } else {
            nomeJogador.style.marginTop = '-66px';
        }
    } else {
        nomeJogador.style.marginTop = '0px';
    }
}

corrigirDivisaoUsuario(document.querySelector('.divisao-jogador'), document.querySelector('.nome-jogador'));

window.addEventListener('resize', function () {
    corrigirDivisaoUsuario(document.querySelector('.divisao-jogador'), document.querySelector('.nome-jogador'));
});

function carregarDetalhesPartida(gameId) {
    let idDiv = '#detalhes-partida-' + gameId;

    if ($(idDiv).first().is(":hidden")) {
        $.ajax({
            method: "GET",
            url: BASE + '/' + gameId + '/' + CONTAID
        }).done(function (data) {

            var html = '<div class="col-lg-6 time time-aliado">';
            html += montaHtmlTime(data.timeAliado, true);
            html += '</div>'
            html += '<div class="col-lg-6 time time-inimigo">'
            html += montaHtmlTime(data.timeInimigo, false);
            html += '</div>'

            $(idDiv).html(html);
            $(idDiv).slideDown("slow", function () {

            });
        });
    } else {
        $(idDiv).slideUp();
    }
}

function montaHtmlTime(time, aliado) {

    let numeroTime = aliado ? '100' : '200';

    let objetivo =
        `<div class="row objetivos">
            <div class="col-2">
                <img src="/images/turret_${numeroTime}.png" title="Torres" />
                <span>${time.numeroTorres}</span>
            </div>
            <div class="col-2">
                <img src="/images/inhibitor_building_${numeroTime}.png" title="Inibidores" />
                <span>${time.numeroInibidores}</span>
            </div>
            <div class="col-2">
                <img src="/images/riftherald_${numeroTime}.png" title="Arautos" />
                <span>${time.numeroArautos}</span>
            </div>
            <div class="col-2">
                <img src="/images/dragon_${numeroTime}.png" title="Dragões" />
                <span>${time.numeroDragoes}</span>
            </div>
            <div class="col-2">
                <img src="/images/baron_nashor_${numeroTime}.png" title="Barões" />
                <span>${time.numeroBaroes}</span>
            </div>
        </div>`;

    let participantes = '';
    time.participantes.forEach(p => {
        let htmlItens = montaHtmlItens(p.itensFinais);
        participantes +=
            `<div class="row">
                    <div class="col-md-12 linha-campeao">
                        <div class="row">
                            <div class="col-md-2 col-sm-3 col-4">
                                <img class="imagem-campeao-detalhes" src="${p.urlIconeCampeao}" title="${p.nomeCampeao}">
                            </div>
                            <div class="col-md-9 col-6 container-detalhes">
                                <div class="row">
                                    <div class="col-xs-12">${p.nomeJogador}</div>
                                </div>
                                <div class="row detalhes-superior">
                                    <div class="col-xs-12 col-md-3">KDA: ${p.kda}</div>
                                    <div class="col-xs-12 col-md-3">Lane: ${p.lane}</div>
                                    <div class="col-xs-12 col-md-3">Nível: ${p.nivelMaximoAtingido}</div>
                                    <div class="col-xs-12 col-md-3">Ouro: ${p.ouroAcumulado}</div>
                                </div>
                                <div class="row detalhes-inferior">
                                    <div class="col-xs-12 col-md-3">Elo: ${p.elo}</div>
                                    <div class="col-xs-12 col-md-3">Farm: ${p.farm}</div>
                                    <div class="col-xs-12 col-md-3">Farm/Min: ${p.farmPorMinuto}</div>
                                    <div class="col-xs-12 col-md-3">Ouro/Min: ${p.ouroPorMinuto}</div>
                                </div>
                                <div class="row">
                                    <div class="container-itens">
                                       ${htmlItens}
                                    </div>
                                    <div class="container-item container-ward">
                                        <img class="item" src="${p.ward.urlIcone}" title="${p.ward.nome}">
                                    </div>
                                </div>
                            </div>
                            <div class="col-xs-1">
                                <div class="">
                                    <img class="item" src="${p.feitico1.urlIcone}" title="${p.feitico1.nome}">
                                </div>
                                <div class="">
                                    <img class="item" src="${p.feitico2.urlIcone}" title="${p.feitico1.nome}">
                                </div>
                            </div>
                        </div>
                    </div>
            </div>`;
    });
    return objetivo + participantes;
}

function montaHtmlItens(itens) {
    let html = '';
    itens.forEach(i =>
        html +=
        `<div class="container-item">
                    <img class="item" src="${i.urlIcone}" title="${i.nome}">
                </div>`);

    return html;
}