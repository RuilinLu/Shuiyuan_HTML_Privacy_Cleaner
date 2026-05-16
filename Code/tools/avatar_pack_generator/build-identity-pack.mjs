import { createWriteStream, mkdirSync, statSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { createGzip } from 'node:zlib';
import { pipeline } from 'node:stream/promises';
import { Readable } from 'node:stream';
import { createAvatar } from '@dicebear/core';
import * as styles from '@dicebear/collection';
import {
  faker,
  fakerDE,
  fakerEN,
  fakerES,
  fakerFR,
  fakerIT,
  fakerJA,
  fakerKO,
  fakerZH_CN,
  fakerZH_TW,
} from '@faker-js/faker';

const count = Number.parseInt(process.env.IDENTITY_PACK_COUNT || '100000', 10);
const output = resolve(process.env.IDENTITY_PACK_OUT || '../../assets/anonymous_identity_pack.jsonl.gz');
const metadataOutput = resolve(process.env.IDENTITY_PACK_META_OUT || '../../assets/anonymous_identity_pack.metadata.json');

const avatarStyles = [
  'adventurer',
  'adventurerNeutral',
  'avataaars',
  'avataaarsNeutral',
  'bigEars',
  'bigEarsNeutral',
  'bigSmile',
  'bottts',
  'botttsNeutral',
  'croodles',
  'croodlesNeutral',
  'dylan',
  'funEmoji',
  'glass',
  'icons',
  'identicon',
  'initials',
  'lorelei',
  'loreleiNeutral',
  'micah',
  'miniavs',
  'notionists',
  'notionistsNeutral',
  'openPeeps',
  'personas',
  'pixelArt',
  'pixelArtNeutral',
  'rings',
  'shapes',
  'thumbs',
  'toonHead',
];

const fakerLocales = [
  fakerZH_CN,
  fakerZH_TW,
  fakerEN,
  fakerJA,
  fakerKO,
  fakerFR,
  fakerDE,
  fakerES,
  fakerIT,
];

const chineseAdjectives = [
  '晴野', '星河', '青岚', '松间', '白昼', '云径', '南风', '海盐', '冬灯', '橙月',
  '竹影', '远山', '微澜', '银杏', '旧港', '夏至', '蓝桥', '晨雾', '秋声', '暖岛',
  '流萤', '月白', '雨巷', '长鲸', '薄荷', '琥珀', '森屿', '观星', '知北', '拾光',
];

const chineseNouns = [
  '档案员', '记录者', '邮差', '观察者', '读书人', '修表匠', '航海家', '云端客', '手账师', '剪影师',
  '巡游者', '调色盘', '造句者', '收藏家', '守夜人', '拾荒诗人', '行星站', '地图师', '漫游人', '灯塔员',
  '探路者', '旧书签', '风铃铺', '玻璃猫', '桃花源', '月台票', '星图册', '纸飞机', '小剧场', '白噪音',
];

const asciiLeft = [
  'aurora', 'cobalt', 'harbor', 'pixel', 'maple', 'orbit', 'cedar', 'silver', 'mint', 'ember',
  'paper', 'stone', 'cloud', 'sunset', 'meadow', 'breeze', 'lantern', 'willow', 'marble', 'forest',
  'violet', 'copper', 'atlas', 'raven', 'nova', 'garden', 'signal', 'river', 'velvet', 'comet',
];

const asciiRight = [
  'note', 'field', 'bridge', 'signal', 'lane', 'studio', 'garden', 'reader', 'runner', 'pilot',
  'canvas', 'thread', 'anchor', 'window', 'compass', 'planet', 'folder', 'circle', 'branch', 'mirror',
  'valley', 'screen', 'archive', 'marker', 'station', 'corner', 'parcel', 'harvest', 'syntax', 'legend',
];

function cleanDisplayName(value) {
  return String(value || '')
    .replace(/[<>{}"'`\\]/g, '')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 32);
}

function cleanUsername(value, index) {
  const cleaned = String(value || '')
    .normalize('NFKD')
    .replace(/[^\w.-]+/g, '_')
    .replace(/^[_\W]+|[_\W]+$/g, '')
    .replace(/_{2,}/g, '_')
    .toLowerCase()
    .slice(0, 28);
  return `${cleaned || 'anonymous'}_${index.toString(36).padStart(4, '0')}`;
}

function buildDisplayName(index) {
  const zero = index - 1;
  const locale = fakerLocales[zero % fakerLocales.length];
  locale.seed(910000 + index);
  faker.seed(710000 + index);

  const mode = zero % 8;
  if (mode === 0) {
    return cleanDisplayName(chineseAdjectives[zero % chineseAdjectives.length] + chineseNouns[Math.floor(zero / chineseAdjectives.length) % chineseNouns.length]);
  }
  if (mode === 1) {
    return cleanDisplayName(locale.person.fullName());
  }
  if (mode === 2) {
    return cleanDisplayName(faker.word.adjective() + ' ' + faker.word.noun());
  }
  if (mode === 3) {
    return cleanDisplayName(chineseAdjectives[(zero * 7) % chineseAdjectives.length] + locale.person.firstName());
  }
  if (mode === 4) {
    return cleanDisplayName(locale.person.firstName() + ' ' + faker.color.human());
  }
  if (mode === 5) {
    return cleanDisplayName(chineseNouns[(zero * 11) % chineseNouns.length] + index.toString().padStart(5, '0'));
  }
  if (mode === 6) {
    return cleanDisplayName(faker.location.city() + ' ' + faker.word.noun());
  }
  return cleanDisplayName(locale.person.fullName() + ' ' + (zero % 97).toString().padStart(2, '0'));
}

function buildUsername(index) {
  const zero = index - 1;
  faker.seed(810000 + index);
  const mode = zero % 5;
  if (mode === 0) {
    return cleanUsername(`${asciiLeft[zero % asciiLeft.length]}_${asciiRight[(zero * 7) % asciiRight.length]}`, index);
  }
  if (mode === 1) {
    return cleanUsername(faker.internet.username(), index);
  }
  if (mode === 2) {
    return cleanUsername(`${faker.word.adjective()}_${faker.word.noun()}`, index);
  }
  if (mode === 3) {
    return cleanUsername(`${asciiRight[(zero * 5) % asciiRight.length]}.${asciiLeft[(zero * 13) % asciiLeft.length]}`, index);
  }
  return cleanUsername(`anon-${faker.string.alphanumeric({ length: 9, casing: 'lower' })}`, index);
}

function buildSvg(index) {
  const zero = index - 1;
  const styleName = avatarStyles[zero % avatarStyles.length];
  const style = styles[styleName];
  const seed = `shuiyuan-archive-anonymous-${styleName}-${index}`;
  const avatar = createAvatar(style, {
    seed,
    size: 96,
    radius: 50,
  });
  const svg = avatar
    .toString()
    .replace(/<metadata[\s\S]*?<\/metadata>/gi, '')
    .replace(/<!--[\s\S]*?-->/g, '');
  return {
    styleName,
    seed,
    svg,
  };
}

async function* records() {
  for (let index = 1; index <= count; index += 1) {
    const avatar = buildSvg(index);
    const record = {
      i: index,
      d: buildDisplayName(index),
      u: buildUsername(index),
      s: avatar.styleName,
      a: avatar.svg,
    };
    yield JSON.stringify(record) + '\n';

    if (index % 5000 === 0) {
      process.stderr.write(`generated ${index}/${count}\n`);
    }
  }
}

mkdirSync(dirname(output), { recursive: true });
mkdirSync(dirname(metadataOutput), { recursive: true });

await pipeline(
  Readable.from(records()),
  createGzip({ level: 9 }),
  createWriteStream(output),
);

const bytes = statSync(output).size;
writeFileSync(metadataOutput, JSON.stringify({
  count,
  generatedAt: new Date().toISOString(),
  avatarSource: 'DiceBear @dicebear/core and @dicebear/collection',
  nameSource: '@faker-js/faker plus local nickname word lists',
  runtimeNetwork: false,
  styles: avatarStyles,
  outputBytes: bytes,
}, null, 2) + '\n', 'utf8');

console.log(`Wrote ${count} identities to ${output}`);
console.log(`Compressed size: ${bytes} bytes`);
