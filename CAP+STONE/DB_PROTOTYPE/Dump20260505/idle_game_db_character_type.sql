-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: idle_game_db
-- ------------------------------------------------------
-- Server version	8.0.44

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `character_type`
--

DROP TABLE IF EXISTS `character_type`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `character_type` (
  `character_type_id` int NOT NULL AUTO_INCREMENT,
  `type_code` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `type_name` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `main_effect` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`character_type_id`),
  UNIQUE KEY `type_code` (`type_code`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `character_type`
--

LOCK TABLES `character_type` WRITE;
/*!40000 ALTER TABLE `character_type` DISABLE KEYS */;
INSERT INTO `character_type` VALUES (1,'ACTIVITY','활동형','게임을 켜둔 상태에서 성장 효율이 높은 캐릭터 유형','게임이 켜진 동안 무기 및 캐릭터 수치 가산, 뽑기 및 강화 비용 감소'),(2,'CONTROL','절제형','스마트폰 미사용 및 절제 행동에 보상을 주는 캐릭터 유형','모험 및 골드의 탑 수확율 증가, 10분간 미조작 시 다음 소모 비용 감소'),(3,'QUEST','퀘스트형','일일 퀘스트 수행에 특화된 캐릭터 유형','일일 퀘스트 개수 증가, 퀘스트 보상 증가'),(4,'EXIT','종료형','게임 종료 중 자동 보상에 특화된 캐릭터 유형','게임 종료 중 자동사냥 보상 증가, 퀘스트 성공 시 타 유형 컨디션 회복율 증가');
/*!40000 ALTER TABLE `character_type` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-05-05 18:08:33
