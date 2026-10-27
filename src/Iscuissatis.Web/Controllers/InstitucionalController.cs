using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Controllers;

public sealed record LegalSection(string Heading, string Body);

public sealed record LegalPage(string Slug, string Title, string Intro, IReadOnlyList<LegalSection> Sections);

/// <summary>
/// Páginas institucionais. O texto é provisório e está marcado como tal na tela:
/// conteúdo jurídico definitivo precisa de revisão do responsável.
/// </summary>
[AllowAnonymous]
public class InstitucionalController : Controller
{
    private static readonly Dictionary<string, LegalPage> Pages = new()
    {
        ["/sobre"] = new LegalPage("/sobre", "Sobre a saboaria",
            "A Iscuissati's Saboaria Artesanal produz sabonetes artesanais em pequenos lotes, em Tatuí, SP.",
            [
                new("Como produzimos", "Cada barra é feita à mão, com ingredientes selecionados e cura acompanhada."),
                new("Nossa história", "Texto institucional definitivo a ser escrito pela saboaria."),
            ]),
        ["/contato"] = new LegalPage("/contato", "Fale com a saboaria",
            "O atendimento é direto, por WhatsApp.",
            [
                new("WhatsApp", "Número a definir."),
                new("E-mail", "Endereço a definir."),
                new("Instagram", "Perfil a definir."),
                new("Horário", "Segunda a sexta, horário comercial — a confirmar."),
            ]),
        ["/faq"] = new LegalPage("/faq", "Perguntas frequentes",
            "As dúvidas mais comuns sobre produtos, prazos e envio.",
            [
                new("Os sabonetes são naturais?", "As fórmulas usam ingredientes selecionados; a composição completa (INCI) está na página de cada produto."),
                new("Quanto tempo dura uma barra?", "Depende do uso e do armazenamento. Manter seca entre os usos prolonga a duração."),
                new("Vocês entregam para todo o Brasil?", "O frete é calculado pelo CEP no checkout."),
                new("Posso retirar em Tatuí?", "Sim, quando a retirada local está habilitada no checkout."),
            ]),
        ["/trocas-e-devolucoes"] = new LegalPage("/trocas-e-devolucoes", "Trocas e devoluções",
            "Regras de troca, devolução e direito de arrependimento.",
            [
                new("Direito de arrependimento", "Compras online podem ser desistidas em até 7 dias corridos após o recebimento (CDC, art. 49), com devolução do valor pago."),
                new("Produto com defeito", "Fale com a saboaria em até 30 dias do recebimento para troca ou reembolso."),
                new("Como solicitar", "O canal de atendimento será informado aqui. Procedimento definitivo a ser fechado com a saboaria."),
            ]),
        ["/privacidade"] = new LegalPage("/privacidade", "Política de privacidade",
            "Como tratamos seus dados pessoais, conforme a LGPD (Lei 13.709/2018).",
            [
                new("Dados coletados", "Nome, e-mail, endereço e telefone, necessários para processar o pedido e entregar."),
                new("Finalidade", "Execução do contrato de compra, obrigações fiscais e atendimento."),
                new("Compartilhamento", "Transportadora e meio de pagamento, apenas o necessário para a entrega e a cobrança."),
                new("Seus direitos", "Você pode exportar ou solicitar a exclusão dos seus dados na área Minha conta."),
                new("Cookies", "Usamos cookies essenciais para a sacola e o login. Cookies de análise só são ativados com o seu consentimento."),
                new("Encarregado (DPO)", "Contato a definir antes da publicação."),
            ]),
        ["/termos"] = new LegalPage("/termos", "Termos de uso",
            "Condições de uso da loja e de compra dos produtos.",
            [
                new("Preços e disponibilidade", "Os preços valem para o momento da compra e podem mudar sem aviso; o pedido confirmado mantém o valor pago."),
                new("Prazos", "O prazo de entrega soma o tempo de preparo ao prazo da transportadora."),
                new("Uso do site", "É proibido usar a loja para fins ilícitos ou tentar burlar mecanismos de segurança."),
                new("Dados da empresa", "Razão social, CNPJ e endereço a serem informados antes da publicação."),
            ]),
    };

    [HttpGet("/sobre")]
    [HttpGet("/contato")]
    [HttpGet("/faq")]
    [HttpGet("/trocas-e-devolucoes")]
    [HttpGet("/privacidade")]
    [HttpGet("/termos")]
    public IActionResult Show()
    {
        var path = Request.Path.Value ?? "/sobre";

        if (!Pages.TryGetValue(path, out var page))
        {
            return NotFound();
        }

        ViewData["Title"] = page.Title;
        ViewData["Description"] = page.Intro;

        return View("Pagina", page);
    }
}
