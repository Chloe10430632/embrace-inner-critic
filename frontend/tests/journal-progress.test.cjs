const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const test = require('node:test')
const vm = require('node:vm')
const ts = require('typescript')

function createStore() {
  const requests = []
  let fail = false
  const source = fs.readFileSync(path.join(__dirname, '../app/stores/journal.ts'), 'utf8')
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText
  const context = {
    exports: {},
    ref: value => ({ value }),
    reactive: value => value,
    computed: fn => ({ get value() { return fn() } }),
    defineStore: (_, setup) => setup,
    useOnboardingStore: () => ({ confirmedName: 'test' }),
    useAuthStore: () => ({ requireUser: async () => true }),
    useAudio: () => ({ playEffect() {} }),
    useNuxtApp: () => ({ $api: async (_, options) => {
      if (fail) throw new Error('save failed')
      requests.push(options.body)
      return { id: 'entry', createdAt: '2026-10-07T00:00:00Z', ...options.body }
    } })
  }
  vm.runInNewContext(compiled, context)
  return { store: context.exports.useJournalStore(), requests, failNextSave() { fail = true } }
}

test('saving awareness records does not complete the progress', async () => {
  const { store, requests } = createStore()
  store.draft.trigger = 'event'
  assert.equal(await store.save(false, 14), true)
  assert.equal(requests[0].isComplete, false)
  assert.ok(requests[0].answers.recordedAt)
  assert.equal(store.entryStatus(store.entries.value[0]), '已記錄')
  assert.equal(store.isRecorded(store.entries.value[0]), true)
})

test('saving an action waits for confirmation after reopening', async () => {
  const { store, requests } = createStore()
  store.draft.nextAction = 'open the document'
  assert.equal(await store.save(false, 23), true)
  assert.equal(requests[0].isComplete, false)
  assert.equal(store.entryStatus(store.entries.value[0]), '待完成行動')
  store.openProgress(store.entries.value[0])
  assert.equal(store.stage.value, 23)
  assert.equal(store.draft.nextAction, 'open the document')
})

test('only saving the final feeling completes progress', async () => {
  const { store, requests } = createStore()
  store.draft.nextAction = 'open the document'
  assert.equal(await store.save(true, 22), false)
  assert.equal(requests.length, 0)
  store.draft.afterActionEmotion = 'still nervous'
  assert.equal(await store.save(true, 22), true)
  assert.equal(requests[0].isComplete, true)
  assert.ok(requests[0].answers.progressCompletedAt)
  assert.equal(store.entryStatus(store.entries.value[0]), '已完成')
  store.openProgress(store.entries.value[0])
  assert.equal(store.stage.value, 21)
})

test('an unfinished action draft returns to planning instead of claiming action started', async () => {
  const { store } = createStore()
  await store.save(false, 14)
  store.draft.nextAction = 'unfinished plan'
  await store.save(false, 6)
  assert.equal(store.entryStatus(store.entries.value[0]), '已記錄')
  store.openProgress(store.entries.value[0])
  assert.equal(store.stage.value, 20)
})

test('legacy records remain accessible and preserve historical answers', async () => {
  const { store } = createStore()
  const legacy = { id: 'old', createdAt: '2026-09-01T00:00:00Z', isComplete: true, answers: { reply: 'historical reply' } }
  assert.equal(store.entryStatus(legacy), '已記錄')
  store.openProgress(legacy)
  assert.equal(store.stage.value, 17)
  assert.equal(await store.save(false, 6), true)
  assert.equal(store.isRecorded(store.entries.value[0]), true)
  assert.equal(store.draft.reply, 'historical reply')
  assert.equal(store.isProgressComplete({ ...legacy, answers: { afterActionEmotion: 'calmer' } }), true)
})

test('failed action saves keep the user on the form without marking progress', async () => {
  const { store, failNextSave } = createStore()
  store.stage.value = 20
  assert.equal(await store.save(false, 23), false)
  store.draft.nextAction = 'open the document'
  failNextSave()
  assert.equal(await store.save(false, 23), false)
  assert.equal(store.stage.value, 20)
  assert.equal(store.draft.actionStartedAt, '')
  assert.equal(store.completed.value, false)
})

test('list filters exact statuses and sorts timestamps without changing stored order', () => {
  const { store } = createStore()
  const fixture = (id, createdAt, updatedAt, answers = {}, isComplete = false) => ({
    id, createdAt, updatedAt, isComplete, answers
  })
  store.entries.value = [
    fixture('recorded', '2026-01-01', '2026-01-04', { recordedAt: '2026-01-01' }),
    fixture('complete', '2026-01-03', '2026-01-03', { recordedAt: '2026-01-03', progressCompletedAt: '2026-01-03' }),
    fixture('action', '2026-01-02', '2026-01-05', { recordedAt: '2026-01-02', actionStartedAt: '2026-01-05' }),
    fixture('draft', '2026-01-04', '2026-01-04'),
    fixture('legacy', '2026-01-06', undefined, { afterActionEmotion: 'calmer' }, true)
  ]
  const ids = (status, order, progressOnly = false) => Array.from(store.listEntries(status, order, progressOnly), entry => entry.id)
  assert.deepEqual(ids('已記錄', 'updated-desc'), ['recorded'])
  assert.deepEqual(ids('待完成行動', 'updated-desc'), ['action'])
  assert.deepEqual(ids('已完成', 'updated-desc'), ['legacy', 'complete'])
  assert.deepEqual(ids('草稿', 'updated-desc'), ['draft'])
  assert.deepEqual(ids('', 'created-asc'), ['recorded', 'action', 'complete', 'draft', 'legacy'])
  assert.deepEqual(ids('', 'created-desc'), ['legacy', 'draft', 'complete', 'action', 'recorded'])
  assert.deepEqual(ids('', 'updated-asc'), ['complete', 'recorded', 'draft', 'action', 'legacy'])
  assert.deepEqual(ids('', 'updated-desc'), ['legacy', 'action', 'recorded', 'draft', 'complete'])
  assert.deepEqual(ids('草稿', 'updated-desc', true), [])
  assert.deepEqual(ids('', 'updated-desc', true), ['legacy', 'action', 'recorded', 'complete'])
  assert.deepEqual(Array.from(store.entries.value, entry => entry.id), ['recorded', 'complete', 'action', 'draft', 'legacy'])
})
