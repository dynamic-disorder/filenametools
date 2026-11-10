#!/usr/bin/env node
// collect-context.mjs — compact, net-vs-HEAD change report for the git-commit skill.
//
// Deciding what to commit needs to know what changed, how much, and in which area — not the text
// of every file, and not the sequence of edits behind it. This prints the net change against
// HEAD, grouped by area, with a hard budget on output size. Git and the Node stdlib only.
//
//   node collect-context.mjs                 everything uncommitted, budgeted diffs
//   node collect-context.mjs --summary       counts and groups, no diff text
//   node collect-context.mjs --path <file>   one path (repeatable; plain paths work too)
//   node collect-context.mjs --staged        the index instead of the worktree
//   node collect-context.mjs --json          machine-readable, no diff text
//
// Exit codes: 0 report produced (including "nothing to commit"), 2 usage, 1 git error.

import { execFileSync } from 'node:child_process';
import { readFileSync, statSync } from 'node:fs';
import { basename, isAbsolute, relative, resolve, sep } from 'node:path';

const EMPTY_TREE = '4b825dc642cb6eb9a060e54bf8d69288fbee4904';
const MAX_PER_FILE = 60;
const MAX_TOTAL = 400;

const USAGE = `usage: node collect-context.mjs [options] [path ...]

  --summary                 counts and groups only; no diff text
  --staged                  report the index instead of the worktree
  --path <p>                limit to a path; repeatable, plain paths work too
  --max-lines-per-file <n>  diff lines per file (default ${MAX_PER_FILE})
  --max-lines <n>           total diff lines (default ${MAX_TOTAL})
  --json                    machine-readable summary, no diff text
  -h, --help                this message`;

// ---------- git ----------

const repoRoot = (() => {
  try {
    return execFileSync('git', ['rev-parse', '--show-toplevel'], {
      cwd: process.cwd(),
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe'],
    }).trim();
  } catch {
    console.error('collect-context: not inside a git working tree');
    process.exit(1);
  }
})();

function git(args, { allowFailure = false } = {}) {
  try {
    return execFileSync('git', args, {
      cwd: repoRoot,
      encoding: 'utf8',
      maxBuffer: 256 * 1024 * 1024,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
  } catch (error) {
    if (allowFailure) return '';
    console.error(`collect-context: git ${args.join(' ')} failed: ${String(error.stderr ?? '').trim()}`);
    process.exit(1);
  }
}

// ---------- arguments ----------

function parseArgs(argv) {
  const options = { paths: [], summary: false, staged: false, json: false, perFile: MAX_PER_FILE, total: MAX_TOTAL };

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '-h' || arg === '--help') {
      console.log(USAGE);
      process.exit(0);
    } else if (arg === '--summary') options.summary = true;
    else if (arg === '--staged') options.staged = true;
    else if (arg === '--json') options.json = true;
    else if (arg === '--path') {
      if (!argv[i + 1]) usageError('--path needs a value');
      options.paths.push(argv[++i]);
    } else if (arg.startsWith('--path=')) options.paths.push(arg.slice(7));
    else if (arg === '--max-lines-per-file' || arg === '--max-lines') {
      const value = Number.parseInt(argv[++i] ?? '', 10);
      if (!Number.isFinite(value) || value < 0) usageError(`${arg} needs a non-negative number`);
      if (arg === '--max-lines') options.total = value;
      else options.perFile = value;
    } else if (arg === '--') {
      options.paths.push(...argv.slice(i + 1));
      break;
    } else if (arg.startsWith('-')) usageError(`unknown option '${arg}'`);
    else options.paths.push(arg);
  }

  return options;
}

function usageError(message) {
  console.error(`collect-context: ${message}\n\n${USAGE}`);
  process.exit(2);
}

// Repo-relative and forward-slashed, matching how git reports paths on every platform.
function normalisePath(path) {
  const absolute = isAbsolute(path) ? path : resolve(process.cwd(), path);
  return relative(repoRoot, absolute).split(sep).join('/') || '.';
}

// ---------- status and line counts ----------

// `git status --porcelain=v2 -z`: NUL-terminated records, space-separated fields.
function readStatus(paths) {
  const args = ['status', '--porcelain=v2', '-z', '--untracked-files=all'];
  if (paths.length) args.push('--', ...paths);

  const records = [];
  const tokens = git(args).split('\0');

  for (let i = 0; i < tokens.length; i++) {
    const token = tokens[i];
    if (!token) continue;

    const kind = token[0];
    const fields = token.split(' ');

    if (kind === '1') records.push({ path: fields.slice(8).join(' '), xy: fields[1] });
    else if (kind === '2') {
      records.push({ path: fields.slice(9).join(' '), origPath: tokens[++i], xy: fields[1], renamed: true });
    } else if (kind === 'u') records.push({ path: fields.slice(10).join(' '), xy: fields[1], unmerged: true });
    else if (kind === '?') records.push({ path: token.slice(2), untracked: true });
  }

  return records;
}

// `git diff --numstat -z`: "adds<TAB>dels<TAB>path"; a rename spends two extra NUL tokens.
function readNumstat(args) {
  const stats = new Map();
  const tokens = git(args).split('\0');
  const count = (value) => (Number.isFinite(Number.parseInt(value, 10)) ? Number.parseInt(value, 10) : 0);

  for (let i = 0; i < tokens.length; i++) {
    const token = tokens[i];
    if (!token) continue;

    const [adds, dels, path] = token.split('\t');

    if (path === '') {
      const origPath = tokens[++i];
      const newPath = tokens[++i];
      stats.set(newPath ?? origPath, { adds: count(adds), dels: count(dels), binary: adds === '-', origPath });
    } else {
      stats.set(path, { adds: count(adds), dels: count(dels), binary: adds === '-' });
    }
  }

  return stats;
}

// ---------- classification ----------

const GENERATED = /(^|\/)(bin|obj|node_modules|\.vs|publish|dist)\//i;
const DOCS = /\.(md|mdx|txt)$/i;
const BUILD = /\.(sln|csproj|fsproj|vbproj|props|targets|editorconfig|gitattributes|gitignore)$/i;
const CONFIG_DIR = /(^|\/)(\.agents|\.claude|\.github|\.githooks)\//i;
const TOOLING_DIR = /(^|\/)(local_dev|local_user_files)\//i;
const SCRIPT = /\.(json|ya?ml|toml|ini|mjs|js|sh|ps1|py)$/i;
const TEST = /(^|\/)tests?\//i;

function kindOf(path) {
  const lower = path.toLowerCase();

  if (GENERATED.test(lower)) return 'generated';
  if (DOCS.test(lower)) return 'docs';
  if (BUILD.test(lower)) return 'build';
  if (CONFIG_DIR.test(lower)) return 'config';
  if (TOOLING_DIR.test(lower)) return 'tooling';
  if (/\.cs$/.test(lower)) return TEST.test(lower) || /tests?\.cs$/.test(lower) ? 'test' : 'code';
  if (SCRIPT.test(lower)) return 'config';

  return 'other';
}

// Two leading directory components name the area; a root file is its own area.
function groupOf(path) {
  const segments = path.split('/');
  segments.pop();

  return segments.length ? segments.slice(0, 2).join('/') : '(root)';
}

function suggestType(kinds) {
  const seen = kinds.filter((kind) => kind !== 'other' && kind !== 'generated');

  if (seen.length === 0) return 'chore';
  if (seen.includes('code')) return 'feat|fix|refactor';
  if (seen.includes('test')) return 'test';
  if (seen.includes('docs')) return 'docs';

  return 'chore';
}

function suggestScope(groupKey) {
  if (groupKey === '(root)') return null;

  const last = groupKey.split('/').pop().toLowerCase().replace(/\.tests$/, '');

  return /^[a-z0-9][a-z0-9._-]*$/.test(last) ? last : null;
}

// ---------- diff text ----------

function diffForPath(path, options) {
  const args = options.staged ? ['diff', '--cached', '-U1'] : ['diff', 'HEAD', '-U1'];

  return git([...args, '--', path], { allowFailure: true });
}

function readUntrackedHead(path, maxLines) {
  try {
    const absolute = resolve(repoRoot, path);
    if (statSync(absolute).size > 1024 * 1024) return ['(large new file)'];

    const content = readFileSync(absolute, 'utf8');
    if (content.includes('\u0000')) return ['(binary new file)'];

    const all = content.split('\n');
    if (all[all.length - 1] === '') all.pop();

    const lines = all.slice(0, maxLines).map((line) => `+${line}`);
    if (all.length > maxLines) lines.push(`+… ${all.length - maxLines} more lines`);

    return lines;
  } catch {
    return ['(unreadable new file)'];
  }
}

// Hunk headers plus the first lines of each hunk identify a change; the rest is volume.
function trimDiff(diffText, maxLines) {
  const kept = [];

  for (const line of diffText.split('\n')) {
    if (/^(diff --git|index |--- |\+\+\+ )/.test(line)) continue;
    if (kept.length >= maxLines) {
      kept.push(`… trimmed at ${maxLines} lines`);
      return kept;
    }
    kept.push(line);
  }

  while (kept.length && kept[kept.length - 1] === '') kept.pop();

  return kept;
}

// ---------- collection ----------

function collect(options) {
  const paths = options.paths.map(normalisePath);
  const hasHead = git(['rev-parse', '--verify', 'HEAD'], { allowFailure: true }).trim() !== '';
  const base = hasHead ? 'HEAD' : EMPTY_TREE;

  const records = readStatus(paths);
  const recordByPath = new Map(records.map((record) => [record.path, record]));

  const numstatArgs = options.staged ? ['diff', '--cached', '--numstat', '-z'] : ['diff', base, '--numstat', '-z'];
  const whitespaceArgs = options.staged ? ['diff', '--cached', '-w', '--numstat', '-z'] : ['diff', base, '-w', '--numstat', '-z'];
  if (paths.length) {
    numstatArgs.push('--', ...paths);
    whitespaceArgs.push('--', ...paths);
  }

  const stats = readNumstat(numstatArgs);
  const whitespaceStats = readNumstat(whitespaceArgs);

  const files = [];
  const seen = new Set();

  for (const [path, stat] of stats) {
    seen.add(path);
    files.push(buildFile(path, stat, recordByPath.get(path), whitespaceStats));
  }

  for (const record of records) {
    if (seen.has(record.path)) continue;
    // In index mode only paths whose staged side moved are part of the change.
    if (options.staged && (record.untracked || record.xy[0] === '.')) continue;

    seen.add(record.path);
    files.push(buildFile(record.path, record.untracked ? measureUntracked(record.path) : { adds: 0, dels: 0 }, record, whitespaceStats));
  }

  for (const path of paths) {
    if (!seen.has(path) && !recordByPath.has(path)) {
      files.push({ path, status: 'unchanged', adds: 0, dels: 0, unchanged: true, kind: kindOf(path) });
    }
  }

  const changed = files.filter((file) => !file.unchanged);

  return {
    repo: basename(repoRoot),
    base: hasHead ? git(['rev-parse', '--short', 'HEAD']).trim() : '(no commits yet)',
    branch: git(['rev-parse', '--abbrev-ref', 'HEAD'], { allowFailure: true }).trim() || '(detached)',
    mode: options.staged ? 'index' : 'worktree',
    scope: paths.length ? paths : [options.staged ? '(all staged)' : '(all uncommitted)'],
    totals: { files: changed.length, adds: sum(changed, 'adds'), dels: sum(changed, 'dels') },
    files,
    groups: buildGroups(changed),
    notes: buildNotes(files, records, options),
  };
}

function buildFile(path, stat, record, whitespaceStats) {
  const xy = record?.xy ?? '..';
  const untracked = record?.untracked ?? false;

  return {
    path,
    status: untracked ? '??' : xy,
    adds: stat.adds,
    dels: stat.dels,
    binary: stat.binary ?? false,
    newFile: untracked || xy[0] === 'A' || xy[1] === 'A',
    deleted: xy[0] === 'D' || xy[1] === 'D',
    renamed: record?.renamed ?? false,
    origPath: stat.origPath ?? record?.origPath,
    // Staged and then edited again: the index no longer matches the worktree.
    split: xy[0] !== '.' && xy[0] !== '?' && xy[1] !== '.',
    whitespaceOnly: !untracked && !whitespaceStats.has(path) && stat.adds + stat.dels > 0,
    unchanged: false,
    kind: kindOf(path),
  };
}

function measureUntracked(path) {
  try {
    const absolute = resolve(repoRoot, path);
    if (statSync(absolute).size > 1024 * 1024) return { adds: 0, dels: 0, binary: false };

    const content = readFileSync(absolute, 'utf8');
    if (content.includes('\u0000')) return { adds: 0, dels: 0, binary: true };

    const lines = content.split('\n');
    return { adds: content.endsWith('\n') ? lines.length - 1 : lines.length, dels: 0, binary: false };
  } catch {
    return { adds: 0, dels: 0, binary: false };
  }
}

function sum(files, key) {
  return files.reduce((total, file) => total + (file[key] ?? 0), 0);
}

function buildGroups(files) {
  const byKey = new Map();

  // A generated path is reported in NOTES, never suggested as a commit of its own.
  for (const file of files.filter((item) => item.kind !== 'generated')) {
    const key = groupOf(file.path);
    if (!byKey.has(key)) byKey.set(key, []);
    byKey.get(key).push(file);
  }

  return [...byKey.entries()]
    .map(([key, groupFiles]) => {
      const kinds = [...new Set(groupFiles.map((file) => file.kind))].sort();

      return {
        key,
        kinds,
        files: groupFiles,
        adds: sum(groupFiles, 'adds'),
        dels: sum(groupFiles, 'dels'),
        suggested: { type: suggestType(kinds), scope: suggestScope(key) },
      };
    })
    .sort((a, b) => b.adds + b.dels - (a.adds + a.dels));
}

function buildNotes(files, records, options) {
  const notes = [];
  const list = (predicate) => files.filter(predicate).map((file) => file.path);

  const split = list((file) => file.split);
  if (split.length) {
    const side = options.staged
      ? 'this report is the index, which is older than the worktree — drop --staged to commit the full change'
      : 'this report is the worktree, so the index holds an older version — pass --staged to commit the index instead';
    notes.push(`${split.length} path(s) differ between index and worktree; ${side}: ${split.join(', ')}`);
  }

  for (const [flag, label] of [
    ['whitespaceOnly', 'whitespace-only'],
    ['binary', 'binary'],
  ]) {
    const paths = list((file) => file[flag]);
    if (paths.length) notes.push(`${label}: ${paths.join(', ')}`);
  }

  const generated = list((file) => file.kind === 'generated');
  if (generated.length) {
    notes.push(`generated paths are tracked and changed (usually a build artefact — leave out): ${generated.join(', ')}`);
  }

  const unmerged = records.filter((record) => record.unmerged).map((record) => record.path);
  if (unmerged.length) notes.push(`unmerged, resolve before committing: ${unmerged.join(', ')}`);

  const unchanged = list((file) => file.unchanged);
  if (unchanged.length) {
    notes.push(`no change against ${options.staged ? 'the index' : 'HEAD'}: ${unchanged.join(', ')}`);
  }

  if (files.length === 0) notes.push(options.staged ? 'index is clean against HEAD' : 'working tree is clean against HEAD');

  return notes;
}

// ---------- rendering ----------

function render(report, options) {
  const lines = [
    `repo    ${report.repo}`,
    `head    ${report.base} (${report.branch})`,
    `mode    ${report.mode}`,
    `scope   ${report.scope.join(', ')}`,
    `change  ${report.totals.files} file(s), +${report.totals.adds} -${report.totals.dels}`,
    '',
  ];

  if (report.groups.length === 0) {
    lines.push(report.totals.files === 0 ? 'nothing to commit' : 'no committable group (see NOTES)');
    for (const note of report.notes) lines.push(`  - ${note}`);
    return lines.join('\n');
  }

  lines.push(`GROUPS (${report.groups.length}) — one commit each unless the request says otherwise`);

  report.groups.forEach((group, index) => {
    const scope = group.suggested.scope ? `, scope ${group.suggested.scope}` : '';
    lines.push(`${index + 1}) ${group.key}  [${group.kinds.join(' ')}]  +${group.adds} -${group.dels}`);
    lines.push(`   suggests: type ${group.suggested.type}${scope}`);

    for (const file of group.files) {
      const flags = [
        file.unchanged && 'unchanged',
        file.newFile && 'new',
        file.deleted && 'deleted',
        file.renamed && `from ${file.origPath}`,
        file.binary && 'binary',
        file.whitespaceOnly && 'whitespace-only',
      ].filter(Boolean);

      const counts = file.binary ? 'bin' : `+${file.adds} -${file.dels}`;
      lines.push(`   ${file.status.padEnd(2)} ${file.path}  ${counts}${flags.length ? `  (${flags.join(', ')})` : ''}`);
    }
  });

  if (report.notes.length) {
    lines.push('', 'NOTES');
    for (const note of report.notes) lines.push(`  - ${note}`);
  }

  if (options.summary) return lines.join('\n');

  const label = report.mode === 'index' ? 'index' : 'HEAD';
  lines.push('', `DIFF (net vs ${label}, ≤${options.perFile} per file, ≤${options.total} total)`);

  let budget = options.total;
  let elided = 0;

  for (const file of report.files) {
    if (file.unchanged || file.kind === 'generated') continue;
    if (budget <= 0) {
      elided += 1;
      continue;
    }

    const allow = Math.min(options.perFile, budget);
    const chunk = file.newFile && file.status === '??'
      ? readUntrackedHead(file.path, allow)
      : trimDiff(diffForPath(file.path, options), allow);

    if (!chunk.length) continue;

    lines.push('', `--- ${file.path}`, ...chunk);
    budget -= chunk.length;
  }

  if (elided) {
    lines.push('', `(${elided} file(s) not shown: diff budget reached — rerun with --path <file> or --max-lines <n>)`);
  }

  return lines.join('\n');
}

// ---------- main ----------

const options = parseArgs(process.argv.slice(2));
const report = collect(options);

if (options.json) {
  const brief = (file) => ({
    path: file.path,
    status: file.status,
    kind: file.kind,
    adds: file.adds,
    dels: file.dels,
    newFile: file.newFile ?? false,
    deleted: file.deleted ?? false,
    renamed: file.renamed ?? false,
    split: file.split ?? false,
    unchanged: file.unchanged ?? false,
  });

  const { files, groups, ...head } = report;

  console.log(
    JSON.stringify(
      {
        ...head,
        files: files.map(brief),
        groups: groups.map((group) => ({ ...group, files: group.files.map((file) => file.path) })),
        diff: 'omitted; rerun without --json or with --path <file>',
      },
      null,
      2,
    ),
  );
} else {
  console.log(render(report, options));
}
