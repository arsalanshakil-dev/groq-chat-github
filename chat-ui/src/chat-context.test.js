import { test } from 'node:test';
import assert from 'node:assert/strict';
import { requestMessages } from './chat-context.js';

const history = [
  { role: 'user', content: 'My name is Sam.' },
  { role: 'assistant', content: 'Hi Sam!', contextCount: 1 },
  { role: 'user', content: 'What is my name?' }
];
test('history mode sends prior turns without display annotations', () => {
  assert.deepEqual(requestMessages(history, true), [
    { role: 'user', content: 'My name is Sam.' },
    { role: 'assistant', content: 'Hi Sam!' },
    { role: 'user', content: 'What is my name?' }
  ]);
});
test('no-history mode sends only the latest message and preserves visible history', () => {
  assert.deepEqual(requestMessages(history, false), [{ role: 'user', content: 'What is my name?' }]);
  assert.equal(history.length, 3);
  assert.equal(history[0].content, 'My name is Sam.');
});
