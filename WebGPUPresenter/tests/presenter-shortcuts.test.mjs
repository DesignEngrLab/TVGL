import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';

test('save shortcut uses the presenter button and preserves existing policy keys', async () => {
    class Element { constructor(tagName = 'DIV', inViewer = false) { this.tagName = tagName; this.inViewer = inViewer; } closest() { return this.inViewer ? this : null; } }
    const window = new EventTarget();
    const save = { disabled: false, clicks: 0, click() { this.clicks++; } };
    const document = { dialog: false, querySelector(selector) { return selector === 'dialog[open]' ? (this.dialog ? {} : null) : save; } };
    const context = vm.createContext({ window, document, HTMLElement: Element });
    const source = await readFile(new URL('../Pages/Home.razor.js', import.meta.url), 'utf8');
    const module = new vm.SourceTextModule(source, { context });
    await module.link(() => {}); await module.evaluate();
    const calls = [];
    module.namespace.registerKeyboardShortcuts({ invokeMethodAsync: (...args) => calls.push(args) });
    function key(key, overrides = {}) {
        const event = new Event('keydown', { cancelable: true });
        for (const [name, value] of Object.entries({ key, target: new Element(), ...overrides }))
            Object.defineProperty(event, name, { value });
        window.dispatchEvent(event);
        return event;
    }
    assert.equal(key('s', { ctrlKey: true, altKey: true }).defaultPrevented, true);
    assert.equal(save.clicks, 1);
    assert.equal(key('s', { ctrlKey: true, altKey: true, repeat: true }).defaultPrevented, true);
    assert.equal(save.clicks, 1);
    assert.equal(key('o', { ctrlKey: true, altKey: true }).defaultPrevented, false);
    assert.equal(key('s', { ctrlKey: true, altKey: true, shiftKey: true }).defaultPrevented, false);
    assert.equal(key('s', { ctrlKey: true, altKey: true, target: new Element('INPUT') }).defaultPrevented, false);
    assert.equal(key('s', { ctrlKey: true, altKey: true, target: new Element('INPUT', true) }).defaultPrevented, true);
    document.dialog = true;
    assert.equal(key('s', { ctrlKey: true, altKey: true }).defaultPrevented, false);
    document.dialog = false; save.disabled = true;
    assert.equal(key('s', { ctrlKey: true, altKey: true }).defaultPrevented, false);
    assert.equal(key('c').defaultPrevented, true);
    assert.deepEqual(calls, [['ContinueWithPolicy', 'Clear']]);
    module.namespace.unregisterKeyboardShortcuts();
    assert.equal(key('c').defaultPrevented, false);
});
