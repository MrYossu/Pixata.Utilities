// The styles an editor in iframe mode copies into its iframe, as the page's stylesheets don't reach into it

// The class on the editable document, which the content styles are written against
export const contentClass = "pixata-blazor-editor-document";

// Copies the content styles (every rule in the page's stylesheets that mentions the content class) and the custom
// properties they use into an iframe, so the content looks the same in both edit modes while page styles stay out
export function contentStyles(host, sheets = document.styleSheets) {
  const rules = [];
  for (const sheet of sheets) {
    let cssRules;
    try {
      cssRules = sheet.cssRules;
    } catch {
      continue;
    }
    collectRules(cssRules, rules);
  }
  const css = rules.join("\n");
  const computed = getComputedStyle(host);
  const variables = [...new Set(css.match(/--pixata-blazor-[a-z0-9-]+/gi) || [])]
    .map(name => [name, computed.getPropertyValue(name).trim()])
    .filter(([, value]) => value)
    .map(([name, value]) => `${name}: ${value};`);
  return `:root { ${variables.join(" ")} }\n${css}`;
}

// Adds the rules that mention the content class to rules, from inside @media, @supports, @layer and @import too. A layer
// is left out, as the iframe has nothing else for the rules to be layered with
export function collectRules(cssRules, rules) {
  for (const rule of cssRules) {
    if (rule instanceof CSSStyleRule) {
      if (rule.selectorText.includes(contentClass)) {
        rules.push(rule.cssText);
      }
    } else if (rule instanceof CSSKeyframesRule) {
      if (/ProseMirror|pixata-blazor/.test(rule.name)) {
        rules.push(rule.cssText);
      }
    } else if (window.CSSImportRule && rule instanceof CSSImportRule) {
      // An imported stylesheet's rules are in the import's own sheet rather than in its cssRules, such as pixata.css
      // imported into a layer with "@import url(...) layer(pixata)". A sheet from another origin can't be read, and one that
      // hasn't loaded has no sheet
      let imported;
      try {
        imported = rule.styleSheet?.cssRules;
      } catch {
        continue;
      }
      if (!imported) {
        continue;
      }
      const inner = [];
      collectRules(imported, inner);
      if (!inner.length) {
        continue;
      }
      let css = inner.join("\n");
      if (rule.supportsText) {
        css = `@supports ${rule.supportsText} { ${css} }`;
      }
      const media = rule.media?.mediaText;
      rules.push(media && media !== "all" ? `@media ${media} { ${css} }` : css);
    } else if (rule.cssRules) {
      const inner = [];
      collectRules(rule.cssRules, inner);
      if (!inner.length) {
        continue;
      }
      if (rule instanceof CSSMediaRule) {
        rules.push(`@media ${rule.media.mediaText} { ${inner.join("\n")} }`);
      } else if (window.CSSSupportsRule && rule instanceof CSSSupportsRule) {
        rules.push(`@supports ${rule.conditionText} { ${inner.join("\n")} }`);
      } else {
        rules.push(...inner);
      }
    }
  }
}
