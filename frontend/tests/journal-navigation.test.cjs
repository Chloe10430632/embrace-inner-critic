const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const test = require('node:test')
const vm = require('node:vm')
const ts = require('typescript')

function createPage(push) {
  const opened = []
  let routeWatcher
  const currentRoute = { path: '/journals', query: { tab: 'progress', progress: 'A' } }
  const store = {
    stage: { value: 23 }, message: { value: '' }, entries: { value: [{ id: 'A' }, { id: 'B' }] },
    openProgress: entry => opened.push(entry.id)
  }
  const source = fs.readFileSync(path.join(__dirname, '../app/pages/journals.vue'), 'utf8').split('<script setup lang="ts">')[1].split('</script>')[0]
  const compiled = ts.transpileModule(source + '\nexport { openProgress }; export function markReady() { ready.value = true }', { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText
  const context = {
    exports: {},
    ref: value => ({ value }), computed: fn => ({ get value() { return fn() } }),
    useRoute: () => currentRoute,
    useRouter: () => ({ currentRoute: { value: currentRoute }, push: target => push(target, currentRoute) }),
    useAuthStore: () => ({ user: {} }), useJournalStore: () => store,
    useOnboardingStore: () => ({ confirmedName: { value: 'test' } }), storeToRefs: value => value,
    onMounted() {}, watch(_, callback) { routeWatcher = callback }, onBeforeRouteUpdate() {}, onBeforeRouteLeave() {}
  }
  vm.runInNewContext(compiled, context)
  context.exports.markReady()
  return { openProgress: context.exports.openProgress, opened, syncRoute: () => routeWatcher() }
}

test('cancelled progress navigation cannot replace the currently selected diary', async () => {
  const page = createPage(async () => ({ type: 8 }))
  await page.openProgress({ id: 'B' })
  assert.deepEqual(page.opened, [])
})

test('an older navigation cannot overwrite a newer diary selection', async () => {
  const page = createPage(async (_, route) => { route.query.progress = 'A'; return undefined })
  await page.openProgress({ id: 'B' })
  assert.deepEqual(page.opened, [])
})


test('successful navigation loads the diary selected by the committed URL once', async () => {
  const page = createPage(async (target, route) => { route.query = target.query })
  await page.openProgress({ id: 'B' })
  assert.deepEqual(page.opened, [])
  page.syncRoute()
  assert.deepEqual(page.opened, ['B'])
})
